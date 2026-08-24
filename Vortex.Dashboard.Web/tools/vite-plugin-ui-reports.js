import { appendFileSync, existsSync, readFileSync, writeFileSync } from 'node:fs';
import { resolve } from 'node:path';

// Stores what the in-page reporter collects, and serves it back so the list can be read and
// edited from the page instead of from the file.
//
// JSONL rather than a JSON array: reports arrive one at a time while the dev server is running,
// and appending a line is atomic enough for that. Rewriting a whole array on every report means
// re-reading, parsing and re-serialising a file that grows all session, and a crash mid-write
// loses every report instead of one line. Editing one report does rewrite the file -- that is
// rare and deliberate, unlike arrival.
//
// Serve only. There is no report endpoint in a build, and the reporter itself is behind
// import.meta.env.DEV, so nothing of this ships.
export function uiReports({ file = 'ui-reports.jsonl', route = '/__ui-report' } = {}) {
  return {
    name: 'ui-reports',
    apply: 'serve',
    configureServer(server) {
      const target = resolve(server.config.root, file);

      const read = () => {
        if (!existsSync(target)) return [];

        const lines = readFileSync(target, 'utf8').trim();

        if (!lines) return [];

        let dirty = false;
        const rows = lines.split('\n').map((line) => {
          const r = JSON.parse(line);

          // Reports written before ids existed get one now: an index is not a stable handle
          // once a report in the middle can be deleted.
          if (!r.id) {
            r.id = `r${Date.parse(r.at) || 0}-${Math.random().toString(36).slice(2, 7)}`;
            dirty = true;
          }

          return r;
        });

        if (dirty) writeFileSync(target, rows.map((r) => JSON.stringify(r)).join('\n') + '\n');

        return rows;
      };

      const write = (rows) =>
        writeFileSync(target, rows.length ? rows.map((r) => JSON.stringify(r)).join('\n') + '\n' : '');

      const json = (res, body, code = 200) => {
        res.statusCode = code;
        res.setHeader('content-type', 'application/json');
        res.end(JSON.stringify(body));
      };

      const readBody = (req) =>
        new Promise((done, fail) => {
          let body = '';

          req.on('data', (chunk) => {
            body += chunk;

            // A report carries a DOM excerpt, so it is not tiny -- but it is not a megabyte either.
            if (body.length > 512 * 1024) req.destroy(new Error('trop volumineux'));
          });
          req.on('end', () => done(body));
          req.on('error', fail);
        });

      server.middlewares.use(route, async (req, res) => {
        const url = new URL(req.url ?? '/', 'http://x');
        const id = url.searchParams.get('id');

        try {
          if (req.method === 'GET') {
            // Without the DOM excerpt and the per-element CSS: the page lists reports, it does
            // not need to rebuild them, and those two fields are most of the payload.
            return json(
              res,
              read().map(({ html, elements, ...rest }) => ({
                ...rest,
                where: elements?.flatMap((e) => e.sources ?? []).find((sourcePath) => sourcePath.includes('/pages/'))
                  ?? elements?.flatMap((e) => e.sources ?? [])[0]
                  ?? null,
                count: elements?.length ?? 0,
              })),
            );
          }

          if (req.method === 'POST') {
            const report = JSON.parse(await readBody(req));

            report.at = new Date().toISOString();
            report.id = `r${Date.now()}-${Math.random().toString(36).slice(2, 7)}`;
            appendFileSync(target, JSON.stringify(report) + '\n', 'utf8');
            server.config.logger.info(`[ui-report] ${report.note?.slice(0, 70) ?? '(sans note)'}`);

            return json(res, { id: report.id }, 201);
          }

          if (req.method === 'PATCH') {
            const patch = JSON.parse(await readBody(req));
            const rows = read();
            const row = rows.find((r) => r.id === patch.id);

            if (!row) return json(res, { error: 'inconnu' }, 404);

            if (typeof patch.note === 'string') row.note = patch.note;
            if (typeof patch.done === 'boolean') row.done = patch.done;

            write(rows);

            return json(res, { ok: true });
          }

          if (req.method === 'DELETE') {
            const rows = read();

            write(id ? rows.filter((r) => r.id !== id) : []);
            res.statusCode = 204;

            return res.end();
          }

          res.statusCode = 405;
          res.end();
        } catch (err) {
          server.config.logger.error(`[ui-report] ${req.method} rejected: ${err.message}`);
          json(res, { error: err.message }, 400);
        }
      });
    },
  };
}
