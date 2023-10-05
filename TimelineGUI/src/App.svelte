<script lang="ts">
  import moment from 'moment';
  import TimelineEditor from './lib/TimelineEditor.svelte'
  import { sendMessage, type EditedTaskModel } from './lib/models';

  let rawTasks = [];
  let editedTasksById: { [id: number]: EditedTaskModel } = {};
  (window as any).chrome.webview.addEventListener("message", ev => {
    console.log("received: ", ev.data);
    rawTasks = ev.data.rawTasks ?? rawTasks;

    if (ev.data.editedTasks) {
      editedTasksById = Object.fromEntries(ev.data.editedTasks
        .map(t => [t.id, { ...t, startTime: moment(t.startTime), endTime: moment(t.endTime) }]));
    }
  });
  let selectedDate = moment().startOf('day');
  let view = { startHour: 0, endHour: 24 };
  $: sendMessage("LoadDay", { date: selectedDate.format("YYYY-MM-DD") });

  function parseView(str: string): { startHour: number; endHour: number; } {
    const [start, end] = str.split('-');
    const startHour = Number(start) || 0;
    const endHour = Number(end) || 24;
    return { startHour, endHour };
  }

  function getDirtyTasks() {
    return Object.values(editedTasksById)
      .filter(t => t.hasUnsavedChanges);
  }

  function reload() {
    selectedDate = selectedDate;
  }

  function saveChanges() {
    const tasksWithUnsavedChanges = getDirtyTasks();
    console.log(`saving changes to ${tasksWithUnsavedChanges.length} tasks...`);
    for (const task of tasksWithUnsavedChanges) {
      sendMessage("UpsertTask", { task });
      task.hasUnsavedChanges = false;
    }
    editedTasksById = editedTasksById;
  }
</script>

<main>
  <button on:click={() => selectedDate = selectedDate.clone().subtract(1, 'days')}>Previous day</button>
  <button on:click={() => selectedDate = selectedDate.clone().add(1, 'days')}>Next day</button>
  <button on:click={() => reload()}>Refresh</button>
  <input type="text" value="{view.startHour}-{view.endHour}"
    on:change={e => view = parseView(e.currentTarget.value)}>
  <button on:click={saveChanges}>Save changes</button>

  <TimelineEditor date={selectedDate.clone()}
    {view}
    {rawTasks}
    bind:editedTasksById />
</main>

<style>
</style>
