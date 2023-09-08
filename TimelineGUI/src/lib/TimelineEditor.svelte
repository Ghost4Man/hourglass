<script lang="ts">
  import { onMount } from 'svelte';
  import moment from "moment";
  import { SvelteGantt, SvelteGanttTable, MomentSvelteGanttDateAdapter, SvelteGanttExternal } from 'svelte-gantt';
  import type { SvelteGanttComponent } from 'svelte-gantt/types/gantt';
  import type { SvelteTask, TaskModel } from 'svelte-gantt/types/core/task';

  export let tasks: any[];
  export let date: moment.Moment;
  export let view = { startHour: 0, endHour: 24 };

  let gantt: SvelteGanttComponent;
  let selectedTasks: SvelteTask[];

  const READONLY_RAWDATA_ROW_ID = 0;

  $: {
    const endTimeFallback =
      (date.isSame(moment(), 'day')) ? moment() : date.clone().endOf('day');
    let ganttTasks: TaskModel[] = tasks.map((d, i) => ({
      id: i,
      resourceId: READONLY_RAWDATA_ROW_ID,
      from: moment(d.StartTime),
      to: moment(d.EndTime ?? endTimeFallback),
      label: d.Label,
      html: `<span>${d.Label}</span>`,
    }));
    gantt?.$set({
      tasks: ganttTasks,
      from: date.clone().set('hour', view.startHour).valueOf(),
      to: date.clone().set('hour', view.endHour).valueOf(),
    });
  }

  function isEditable(task: TaskModel) {
    return task.resourceId !== READONLY_RAWDATA_ROW_ID;
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
    gantt.api['tasks'].on.changed(_ => { selectedTasks = selectedTasks; });

    new SvelteGanttExternal(document.getElementById('new-task-button'), {
      gantt,
      enabled: true,
      dragging: false,
      onsuccess: (row, date, gantt) => {
        const id = 5000 + Math.floor(Math.random() * 99999);
        gantt.updateTask({
          id,
          label: `Task #${id}`,
          from: date,
          to: moment(date).clone().add(30, 'minutes'),
          classes: "has-unsaved-changes",
          resourceId: row.model.id as number
        });
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
        on:input={() => gantt.updateTask(task)}></h3>
    {:else}
      <h3 class="selected-task">{task.label}</h3>
    {/if}
    from: {moment(task.from).format("YYYY-MM-DD HH:mm:ss")}<br>
    to: {moment(task.to).format("YYYY-MM-DD HH:mm:ss")}<br>
    duration: {moment.duration(moment(task.from).diff(task.to)).humanize()}
  </aside>
{/each}

<details class="card">
  <summary>Debug info</summary>
  <pre>{JSON.stringify(tasks, null, 4)}</pre>
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
</style>

