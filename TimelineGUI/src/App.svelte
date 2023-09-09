<script lang="ts">
  import moment from 'moment';
  import TimelineEditor from './lib/TimelineEditor.svelte'

  let rawTasks = [];
  (window as any).chrome.webview.addEventListener("message", ev => {
    rawTasks = ev.data;
    //console.log("rawTasks: ", rawTasks);
  });
  let selectedDate = moment().startOf('day');
  let view = { startHour: 0, endHour: 24 };
  $: {
    const msg = selectedDate.format("YYYY-MM-DD");
    console.log("sending:", msg);
    (window as any).chrome.webview.postMessage(msg);
  }

  function parseView(str: string): { startHour: number; endHour: number; } {
    const [start, end] = str.split('-');
    const startHour = Number(start) || 0;
    const endHour = Number(end) || 24;
    return { startHour, endHour };
  }
</script>

<main>
  <button on:click={() => selectedDate = selectedDate.clone().subtract(1, 'days')}>Previous day</button>
  <button on:click={() => selectedDate = selectedDate.clone().add(1, 'days')}>Next day</button>
  <input type="text" value="{view.startHour}-{view.endHour}"
    on:change={e => view = parseView(e.currentTarget.value)}>

  <TimelineEditor date={selectedDate.clone()}
  	{view}
  	{rawTasks} />
</main>

<style>
</style>
