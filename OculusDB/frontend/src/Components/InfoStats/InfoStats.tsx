import RepoEntry from './InfoStats/RepoEntry';

import './InfoStats.css'
import { onMount, For } from 'solid-js';

let InfoStats = () => {
  let dataEntriesCount: HTMLElement;
  let activityEntriesCount: HTMLElement;
  let recognisedAppCount: HTMLElement;

  let changelogs: HTMLElement;

  onMount(() => {
    fetch('https://oculusdb-rewrite.rui2015.me/api/v2/database/info')
      .then(data => data.json())
      .then(data => {
        dataEntriesCount.innerText = data.counts.Application + data.counts.Version + data.counts.IapItem + data.counts.IapItemPack + data.counts.Achievement + data.counts.Offer + data.counts.AppImage + data.counts.Difference + data.counts.VersionAlias;
        activityEntriesCount.innerText = data.counts.Difference;
        recognisedAppCount.innerText = data.counts.Application;
      })

    fetch('https://oculusdb-rewrite.rui2015.me/api/v2/commits')
      .then(data => data.json())
      .then(data => {
        changelogs.innerHTML = '';
        changelogs.appendChild(
          <div>
            <For each={data}>
              { ( record ) =>
                <RepoEntry time={new Date(record.time)} changelog={record.changelog} />
              }
            </For>
          </div> as Node
        )
      })
  })

  return (
    <div class="main">
      <div class="info">
        <h2>General info</h2>
        <p>
          All data is being gathered from Oculus GraphQL api at <a href="https://graph.oculus.com/graphql">https://graph.oculus.com/graphql</a>.
          Oculus DB is <a href="https://github.com/ComputerElite/OculusDB">Open Source</a> and has been created by <a href="https://computerelite.github.io/">ComputerElite</a>
        </p>

        <h2>Database info</h2>
        <div style={{ display: 'flex' }}>
          <div style={{ width: '400px' }}>
            <b>Data Entries</b><hr />
            <b>Activity Entries</b><hr />
            <b>Apps recognized by OculusDB</b>
          </div>
          <div style={{ width: '100%' }}>
            <span ref={( el ) => dataEntriesCount = el}>Loading...</span><hr />
            <span ref={( el ) => activityEntriesCount = el}>Loading...</span><hr />
            <span ref={( el ) => recognisedAppCount = el}>Loading...</span>
          </div>
        </div><br />

        <a href="/downloadstats">Download Stats</a>

        <h2>Recent Server repo Updates</h2>
        <div ref={( el ) => changelogs = el}></div>
      </div>
    </div>
  )
}

export default InfoStats