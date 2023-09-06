<script lang="ts">
  import moment from 'moment';
  import TimelineEditor from './lib/TimelineEditor.svelte'

  let dayData = [];
  (window as any).chrome.webview.addEventListener("message", ev => {
    dayData = ev.data;
    //console.log("dayData: ", dayData);
  });
  let selectedDate = moment().startOf('day');
  $: {
    const msg = selectedDate.format("YYYY-MM-DD");
    console.log("sending:", msg);
    (window as any).chrome.webview.postMessage(msg);
  }
</script>

<main>
  <button on:click={() => selectedDate = selectedDate.clone().subtract(1, 'days')}>Previous day</button>
  <button on:click={() => selectedDate = selectedDate.clone().add(1, 'days')}>Next day</button>

  <TimelineEditor date={selectedDate.clone()} tasks={dayData} />
</main>

<style>
</style>
