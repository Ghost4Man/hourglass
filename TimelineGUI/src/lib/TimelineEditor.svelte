<script lang="ts">
  import { onMount } from 'svelte';
  import moment from "moment";
  import { SvelteGantt, SvelteGanttTable, MomentSvelteGanttDateAdapter, SvelteGanttExternal } from 'svelte-gantt';
  import type { SvelteGanttComponent } from 'svelte-gantt/types/gantt';
  import type { SvelteTask, TaskModel } from 'svelte-gantt/types/core/task';
  import type { EditedTaskModel } from './models';

  export let rawTasks: any[];
  export let editedTasksById: { [id: number]: EditedTaskModel };
  export let date: moment.Moment;
  export let view = { startHour: 0, endHour: 24 };

  let gantt: SvelteGanttComponent;
  let selectedTasks: SvelteTask[];

  const READONLY_RAWDATA_ROW_ID = 0;

  const endTimeFallback =
    (date.isSame(moment(), 'day')) ? moment() : date.clone().endOf('day');

  let rawTasksAsGanttTasks: TaskModel[];
  let editedTasksAsGanttTasks: TaskModel[];

  $: rawTasksAsGanttTasks = rawTasks.map((d, i) => ({
    id: i,
    resourceId: READONLY_RAWDATA_ROW_ID,
    from: moment(d.startTime),
    to: moment(d.endTime ?? endTimeFallback),
    label: d.label,
    html: `<span>${d.label}</span>`,
  }));

  $: editedTasksAsGanttTasks = Object.values(editedTasksById).map(t => ({
    id: t.id,
    resourceId: t.depth,
    from: t.startTime,
    to: t.endTime,
    label: t.label,
    html: `<span>${t.label}</span>`,
    classes: t.hasUnsavedChanges ? ['has-unsaved-changes'] : [],
  }));

  $: gantt?.$set({
    tasks: [...rawTasksAsGanttTasks, ...editedTasksAsGanttTasks],
    from: date.clone().set('hour', view.startHour).valueOf(),
    to: date.clone().set('hour', view.endHour).valueOf(),
  });

  function isEditable(task: TaskModel) {
    return task.resourceId !== READONLY_RAWDATA_ROW_ID;
  }

  function addNewTask(date: moment.Moment, depth: number): EditedTaskModel {
    const id = window.crypto.getRandomValues(new Int32Array(1))[0];
    const newTask: EditedTaskModel = {
      id,
      label: `Task #${id}`,
      startTime: date.clone(),
      endTime: date.clone().add(30, 'minutes'),
      depth,
      tags: "",
      hasUnsavedChanges: true,
    };
    editedTasksById[id] = newTask;
    return newTask;
  }

  function updateTask(task: TaskModel) {
    gantt.updateTask(task);

    editedTasksById[task.id] = {
      id: task.id,
      startTime: moment(task.from),
      endTime: moment(task.to),
      label: task.label,
      tags: "",
      depth: task.resourceId,
      hasUnsavedChanges: true,
      isDeleted: editedTasksById[task.id]?.isDeleted ?? false,
    };
  }

  function deleteTask(task: TaskModel) {
    editedTasksById[task.id].isDeleted = true;
    updateTask(task);
  }

  onMount(() => {
    gantt = new SvelteGantt({
      target: document.getElementById('gantt-view'), 
      props: {
        dateAdapter: new MomentSvelteGanttDateAdapter(moment),
        rowHeight: 100,
        rows: [
          {
            id: READONLY_RAWDATA_ROW_ID,
            label: "raw data",
            height: 100,
            classes: ["row-disabled"],
            enableDragging: false,
          },
          {
            id: 1,
            label: "editable",
            height: 100,
          },
        ],
        from: 0,
        to: 1,
        fitWidth: true,
        magnetUnit: "second",
        magnetOffset: 1,
        columnStrokeColor: "#aaa4",
        onTaskButtonClick(task) {
          console.log(task.model);
        },
      }
    });
    gantt.api['tasks'].on.select(tasks => { selectedTasks = tasks; });
    gantt.api['tasks'].on.changed(moves => {
      moves.forEach(({ task }) => {
        updateTask(task.model);
      });
      selectedTasks = selectedTasks;
    });

    new SvelteGanttExternal(document.getElementById('new-task-button'), {
      gantt,
      enabled: true,
      dragging: false,
      onsuccess: (row, date, gantt) => {
        addNewTask(moment(date), row.model.id as number);
      },
      // called when dragged outside main gantt area
      onfail: () => { },
      // factory function, creates HTMLElement that will follow the mouse
      elementContent: () => {
        const element = document.createElement('div');
        element.innerHTML = 'New Task';
        element.className = 'new-task-drag-ghost';
        return element;
      }
    });
  })
</script>

<div id="gantt-view">
  <div id="new-task-button" class="sg-task">New task</div>
</div>

{#each (selectedTasks ?? []) as {model: task}}
  <aside class="card">
    <span class="selected-task-heading-label">Selected task:</span>
    {#if isEditable(task)}
      <h3 class="selected-task" contenteditable bind:textContent={task.label}
        on:input={() => updateTask(task)}></h3>
    {:else}
      <h3 class="selected-task">{task.label}</h3>
    {/if}
    from: {moment(task.from).format("YYYY-MM-DD HH:mm:ss")}<br>
    to: {moment(task.to).format("YYYY-MM-DD HH:mm:ss")}<br>
    duration: {moment.duration(moment(task.from).diff(task.to)).humanize()}<br>
    actions:
    {#if isEditable(task)}
      <button class="task-action danger" on:click={() => { deleteTask(task); }}>Delete task</button>
    {/if}
  </aside>
{/each}

<details class="card">
  <summary>Debug info</summary>
  <pre>{JSON.stringify(rawTasks, null, 4)}</pre>
</details>

<style>
  pre { font-size: 0.7em; }

  .selected-task-heading-label {
    text-transform: uppercase;
    color: #888e;
    font-size: small;
    display: block;
    line-height: 1;
  }

  #gantt-view :global(.row-disabled) {
    background: repeating-linear-gradient(45deg, #8883, #8883 10px, transparent 10px, transparent 20px);
  }
  
  #gantt-view :global(.sg-task),
  :global(.new-task-drag-ghost) {
    color: currentColor;
    background-color: #3da4fe75;
    white-space: wrap;
    overflow: visible;
    border-radius: 0.5em;
    border: 1px solid #3da4fe;
    line-height: 1.1;
  }
  #gantt-view :global(.sg-task):hover {
    background-color: #3da4fe99;
    z-index: 10;
  }
  #gantt-view :global(.sg-task.has-unsaved-changes) {
    border: 3px dotted #fe3d60;
  }
  #gantt-view :global(.sg-task.selected) {
    border-width: 3px;
    font-weight: bold;
  }

  #gantt-view :global(.sg-task-content) {
    padding: 0;
    position: unset;
    font-size: 0.8em;
    font-family: Arial;
    overflow: hidden;
  }
  #gantt-view :global(.sg-task:hover .sg-task-content) {
    overflow: visible;
    width: auto;
  }
  #gantt-view :global(.sg-task:hover .sg-task-content > span) {
    background: #3da4fe99;
    box-shadow: 0 0 0 4px #3da4fe99;
    backdrop-filter: blur(3px);
    -webkit-box-decoration-break: clone;
    box-decoration-break: clone;
    pointer-events: none;
  }

  #gantt-view :global(.column-header-cell) {
    border-color: #9997;
  }
  #gantt-view :global(.column-header-cell):hover {
    background-color: #9992;
  }

  #new-task-button {
    width: 5em;
    padding: 0.3em;
    cursor: grab;
  }

  :global(.new-task-drag-ghost) {
    position: absolute;
    pointer-events: none;
    cursor: grabbing;
    padding: 0.3em;
  }

  [contenteditable]:hover:not(:focus-within) {
    outline: 1px solid #888a;
  }

  button.task-action {
    padding: 0em 0.5em;
  }
  button.danger:hover {
    color: red;
    border-color: currentColor;
  }
</style>

