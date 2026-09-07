// Generated from the C# response contracts by ApiTypeScriptContractTests.
// Do not edit: change the records, run the test, copy what it writes.

export interface ArticleCategoryOption {
  id: number;
  code: string;
  labels: string;
  sortOrder: number;
  enabled: boolean;
}

export interface ArticleDetail {
  id: number;
  slug: string;
  category: string;
  status: string;
  publishAt: string | null;
  pinned: boolean;
  author: string;
  translations: ArticleTranslation[];
}

export interface ArticleFormMeta {
  categories: ArticleCategoryOption[];
  languages: ArticleLanguageOption[];
  imageBase: string | null;
  imageDirectories: string[];
  blockTypes: string[];
}

export interface ArticleImage {
  path: string;
  thumb: string;
}

export interface ArticleImageBrowse {
  total: number;
  page: number;
  pageSize: number;
  count: number;
  items: ArticleImage[];
  error: string | null;
}

export interface ArticleLanguageOption {
  id: number;
  code: string;
  label: string;
  isDefault: boolean;
  enabled: boolean;
  sortOrder: number;
}

export interface ArticleListItem {
  id: number;
  slug: string;
  category: string;
  status: string;
  scheduled: boolean;
  publishAt: string | null;
  pinned: boolean;
  author: string;
  title: string;
  languages: string[];
}

export interface ArticleListResponse {
  total: number;
  page: number;
  pageSize: number;
  count: number;
  items: ArticleListItem[];
}

export interface ArticleTranslation {
  lang: string;
  title: string;
  summary: string;
  body: string;
  headerImage: string | null;
  thumbnail: string | null;
}

export interface PollChoiceDetail {
  id: number;
  value: string;
  choiceText: string;
  choiceType: number;
  sortOrder: number;
}

export interface PollDetail {
  id: number;
  code: string;
  pollType: string;
  headline: string;
  summary: string;
  startMessage: string;
  endMessage: string;
  npsPoll: boolean;
  enabled: boolean;
  offerOnRoomEntry: boolean;
  roomId: number | null;
  roomName: string | null;
  sortOrder: number;
  questions: PollQuestionNode[];
}

export interface PollFreeTextAnswer {
  playerId: number;
  playerName: string | null;
  avatarUrl: string | null;
  answer: string;
  answeredAt: string;
}

export interface PollFunnel {
  offered: number;
  pending: number;
  started: number;
  completed: number;
  rejected: number;
  completionRate: number;
  rejectionRate: number;
}

export interface PollListItem {
  id: number;
  code: string;
  pollType: string;
  headline: string;
  summary: string;
  startMessage: string;
  endMessage: string;
  npsPoll: boolean;
  enabled: boolean;
  offerOnRoomEntry: boolean;
  roomId: number | null;
  roomName: string | null;
  roomMissing: boolean;
  sortOrder: number;
  rootQuestionCount: number;
  followUpCount: number;
  offeredCount: number;
  startedCount: number;
  completedCount: number;
  rejectedCount: number;
  completionRate: number;
  offerable: boolean;
}

export interface PollListResponse {
  count: number;
  items: PollListItem[];
}

export interface PollQuestionNode {
  id: number;
  sortOrder: number;
  questionType: number;
  questionTypeName: string;
  questionText: string;
  questionCategory: number;
  questionAnswerType: number;
  answerCount: number;
  choices: PollChoiceDetail[];
  children: PollQuestionNode[];
}

export interface PollQuestionResult {
  id: number;
  questionText: string;
  questionType: number;
  questionTypeName: string;
  isFollowUp: boolean;
  parentQuestionId: number | null;
  questionCategory: number;
  respondents: number;
  answerCount: number;
  tally: PollTallyEntry[];
  freeText: PollFreeTextAnswer[];
  freeTextTruncated: boolean;
}

export interface PollQuestionTypeOption {
  id: number;
  name: string;
  supported: boolean;
  takesChoices: boolean;
}

export interface PollQuestionTypeOptions {
  count: number;
  items: PollQuestionTypeOption[];
}

export interface PollResults {
  id: number;
  code: string;
  headline: string;
  npsPoll: boolean;
  funnel: PollFunnel;
  questions: PollQuestionResult[];
}

export interface PollTallyEntry {
  value: string;
  text: string;
  choiceType: number;
  count: number;
  share: number;
  retired: boolean;
}

