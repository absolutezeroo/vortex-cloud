namespace Turbo.Observability.Dashboard;

/// <summary>
/// The single-page admin UI served at <c>/</c>. Intentionally dependency-free (vanilla HTML/JS) and
/// embedded as a constant so the dashboard ships as part of the assembly with no static-file hosting.
/// It reads the admin token from the <c>?token=</c> query string and calls the read-only JSON API.
/// </summary>
internal static class DashboardHtml
{
    public const string Page =
        @"<!doctype html>
<html><head><meta charset='utf-8'><title>Turbo Operations Center</title>
<style>
body{font-family:system-ui,sans-serif;margin:0;background:#0f1115;color:#e6e6e6}
header{background:#161a22;padding:12px 20px;font-weight:600;border-bottom:1px solid #2a2f3a}
main{padding:20px;max-width:1100px;margin:auto}
.cards{display:flex;gap:12px;flex-wrap:wrap;margin-bottom:20px}
.card{background:#161a22;border:1px solid #2a2f3a;border-radius:8px;padding:14px 18px;min-width:130px}
.card b{display:block;font-size:22px}
.muted{color:#8a93a6;font-size:12px}
input,button{background:#1c2230;color:#e6e6e6;border:1px solid #2a2f3a;border-radius:6px;padding:8px 10px;font-size:14px}
button{cursor:pointer}
table{width:100%;border-collapse:collapse;margin-top:10px;font-size:13px}
th,td{text-align:left;padding:6px 8px;border-bottom:1px solid #232834}
th{color:#8a93a6;font-weight:500}
pre{background:#161a22;border:1px solid #2a2f3a;border-radius:8px;padding:14px;overflow:auto;font-size:12px}
h2{font-size:15px;color:#cdd3df;margin:24px 0 6px}
.tag{font-size:11px;padding:2px 6px;border-radius:4px;background:#222a3a}
</style></head>
<body>
<header>Turbo Operations Center <span class='muted' id='status'></span></header>
<main>
<div class='cards' id='cards'></div>
<div><input id='q' placeholder='player id / item id / correlation id' size='40'><button onclick='doSearch()'>Search</button></div>
<div id='search'></div>
<h2>Recent audit events</h2>
<table id='audit'><thead><tr><th>time</th><th>category</th><th>action</th><th>actor</th><th>target</th><th>result</th><th>cid</th></tr></thead><tbody></tbody></table>
</main>
<script>
const token=new URLSearchParams(location.search).get('token')||'';
const api=(p)=>fetch(p,{headers:{'X-Admin-Token':token}}).then(r=>r.json());
const esc=(s)=>(s==null?'':String(s)).replace(/[&<>]/g,c=>({'&':'&amp;','<':'&lt;','>':'&gt;'}[c]));
function card(t,v){return '<div class=card><span class=muted>'+t+'</span><b>'+v+'</b></div>';}
async function load(){
  try{
    const o=await api('/api/overview');
    document.getElementById('status').textContent='· up '+o.uptimeSeconds+'s · '+o.managedMemoryMb+' MB';
    const cats=(o.auditLastHourByCategory||[]).map(c=>c.category+': '+c.count).join(' · ')||'none';
    document.getElementById('cards').innerHTML=
      card('Audit',o.totals.audit)+card('Ledger',o.totals.ledger)+card('Item events',o.totals.items)+
      '<div class=card><span class=muted>last hour</span><b style=font-size:13px>'+esc(cats)+'</b></div>';
    const a=await api('/api/audit?limit=50');
    const tb=document.querySelector('#audit tbody');
    tb.innerHTML=(a.items||[]).map(r=>'<tr><td>'+esc(r.occurredAt)+'</td><td><span class=tag>'+esc(r.category)+'</span></td><td>'+esc(r.action)+'</td><td>'+esc(r.actorPlayerId)+'</td><td>'+esc(r.targetPlayerId)+'</td><td>'+esc(r.result)+'</td><td class=muted>'+esc((r.correlationId||'').slice(0,8))+'</td></tr>').join('');
  }catch(e){document.getElementById('status').textContent='· error (check token)';}
}
async function doSearch(){
  const q=document.getElementById('q').value.trim();
  if(!q)return;
  const r=await api('/api/search?q='+encodeURIComponent(q));
  document.getElementById('search').innerHTML='<h2>Search: '+esc(q)+'</h2><pre>'+esc(JSON.stringify(r,null,2))+'</pre>';
}
load();
</script>
</body></html>";
}
