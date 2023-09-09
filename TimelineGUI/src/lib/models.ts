
export type EditedTaskModel = {
  id: number,
  startTime: moment.Moment,
  endTime: moment.Moment,
  label: string,
  depth: number,
  tags: string,
  hasUnsavedChanges?: boolean,
};


export function sendMessage(msg: "LoadDay", data: { date: string });
export function sendMessage(msg: "UpsertTask", data: { task: EditedTaskModel });

export function sendMessage(msg: string, data: any) {
  const message = { msg, ...data };
  console.log("sending:", message);
  (window as any).chrome.webview.postMessage(message);
}