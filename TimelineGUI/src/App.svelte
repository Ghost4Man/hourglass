<script lang="ts">
  import moment from 'moment';
  import TimelineEditor from './lib/TimelineEditor.svelte'
  import { sendMessage, type EditedTaskModel } from './lib/models';
  import { onMount } from 'svelte';
  import { IconPrevious, IconNext, IconRefresh, IconSaveAll } from './lib/icons';

  const AUTOREFRESH_INTERVAL = 4 * 60 * 1000; // every four minutes

  let rawTasks = [];
  let editedTasksById: { [id: number]: EditedTaskModel } = {};
  (window as any).chrome.webview.addEventListener("message", ev => {
    console.debug("received: ", ev.data);
    rawTasks = ev.data.rawTasks ?? rawTasks;

    if (ev.data.editedTasks) {
      editedTasksById = Object.fromEntries(ev.data.editedTasks
        .map(t => [t.id, { ...t, startTime: moment(t.startTime), endTime: moment(t.endTime) }]));
    }
  });
  let selectedDate = moment().startOf('day');
  let view = { startHour: 0, endHour: 24 };
  $: sendMessage("LoadDay", { date: selectedDate.format("YYYY-MM-DD") });

  onMount(() => {
    const timer = setInterval(performAutorefresh, AUTOREFRESH_INTERVAL)
    return () => clearInterval(timer);
  });

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

  function performAutorefresh() {
    if (getDirtyTasks().length > 0) {
      console.log("skipping autorefresh due to unsaved changes");
    } else {
      console.log("running autorefresh...");
      reload();
    }
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
  <header class="toolbar">
    <button title="Refresh" on:click={() => reload()}>
      <IconRefresh />
    </button>

    <div class="toolbar-group date-selection">
      <button title="Previous day" class="narrow"
          on:click={() => selectedDate = selectedDate.clone().subtract(1, 'days')}>
        <IconPrevious />
      </button>
      <input type="date" name="selectedDate" value={selectedDate.format(moment.HTML5_FMT.DATE)}
        on:input={e => { selectedDate = moment(e.target["valueAsDate"]); }}>
      <button title="Next day" class="narrow"
          on:click={() => selectedDate = selectedDate.clone().add(1, 'days')}>
        <IconNext />
      </button>
    </div>

    <div class="toolbar-group view-settings">
      <input type="text" value="{view.startHour}-{view.endHour}" title="Hour range to display"
        on:change={e => view = parseView(e.currentTarget.value)}>
    </div>

    <button on:click={saveChanges}>
      <IconSaveAll />
      Save changes
    </button>
  </header>

  <TimelineEditor date={selectedDate.clone()}
    {view}
    {rawTasks}
    bind:editedTasksById />
</main>

<style>
  .toolbar {
    padding: 0.3em;
    display: flex;
    border-bottom: 1px solid #ccc5;
    box-shadow: 0 0 20px -3px #0003;
    gap: 1.5em;
  }
  .toolbar-group {
    display: flex;
  }
  input[type=date] {
    text-align: center;
  }
  .view-settings input {
    width: 8ch;
  }
</style>
