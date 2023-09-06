<script lang="ts">
  import { onMount } from 'svelte';
  import moment from "moment";
  import { SvelteGantt, SvelteGanttTable, MomentSvelteGanttDateAdapter } from 'svelte-gantt';
  import type { SvelteGanttComponent } from 'svelte-gantt/types/gantt';
  import type { SvelteTask, TaskModel } from 'svelte-gantt/types/core/task';

  export let tasks: any[];
  export let date: moment.Moment;

  let gantt: SvelteGanttComponent;
  let selectedTasks: SvelteTask[];

  $: {
    const endTimeFallback =
      (date.isSame(moment(), 'day')) ? moment() : date.endOf('day');
    let ganttTasks: TaskModel[] = tasks.map((d, i) => ({
      id: i,
      resourceId: 0,
      from: moment(d.StartTime),
      to: moment(d.EndTime ?? endTimeFallback),
      label: d.Label,
    }));
    gantt?.$set({
      tasks: ganttTasks,
      from: date.startOf('day').valueOf(),
      to: date.endOf('day').valueOf(),
    });
  }

  onMount(() => {
    gantt = new SvelteGantt({
      target: document.getElementById('gantt-view'), 
      props: {
        dateAdapter: new MomentSvelteGanttDateAdapter(moment),
        rowHeight: 150,
        rows: [{
          id: 0,
          label: "some day",
          height: 100,
          classes: ["row-disabled"],
          enableDragging: false,
        }],
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
    gantt.api.tasks.on.select(tasks => { selectedTasks = tasks; });
  })
</script>

<div id="gantt-view"></div>

{#each (selectedTasks ?? []) as {model: task}}
  <aside class="card">
    <h3>{task.label}</h3>
    from: {moment(task.from).format("YYYY-MM-DD HH:mm:ss")}<br>
    to: {moment(task.to).format("YYYY-MM-DD HH:mm:ss")}<br>
    duration: {moment.duration(task.from.diff(task.to)).humanize()}
  </aside>
{/each}

<details class="card">
  <summary>Debug info</summary>
  <pre>{JSON.stringify(tasks, null, 4)}</pre>
</details>

<style>
  pre { font-size: 0.7em; }

  #gantt-view :global(.row-disabled) {
    background: repeating-linear-gradient(45deg, #8883, #8883 10px, transparent 10px, transparent 20px);
  }
  #gantt-view :global(.sg-task) {
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
  }
  #gantt-view :global(.sg-task.selected) {
    border: 2px solid #3da4fe;
    font-weight: bold;
  }
  #gantt-view :global(.sg-task-content) {
    padding: 0;
    position: unset;
    font-size: 0.8em;
    font-family: Arial;
    overflow: hidden;
  }
  #gantt-view :global(.sg-task-content):hover {
    overflow: visible;
    width: auto;
  }
</style>

