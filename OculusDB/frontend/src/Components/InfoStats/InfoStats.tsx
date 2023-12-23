import RepoEntry from './InfoStats/RepoEntry';

import './InfoStats.css'

let InfoStats = () => {
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
            999999999 million<hr />
            99999999 million<hr />
            9999999999 million
          </div>
        </div><br />

        <a href="/downloadstats">Download Stats</a>

        <h2>Recent Server repo Updates</h2>

        <RepoEntry time={new Date('2023-12-02T17:06:07Z')} changelog="something?\n\nFull changes: https://github.com/ComputerElite/OculusDB/commit/83c8c6e35ff3cb0c78f361b114414cf9a83c8299" />
        <RepoEntry time={new Date('2023-12-02T17:06:07Z')} changelog="something?\n\nFull changes: https://github.com/ComputerElite/OculusDB/commit/83c8c6e35ff3cb0c78f361b114414cf9a83c8299" />
        <RepoEntry time={new Date('2023-12-02T17:06:07Z')} changelog="something?\n\nFull changes: https://github.com/ComputerElite/OculusDB/commit/83c8c6e35ff3cb0c78f361b114414cf9a83c8299" />
        <RepoEntry time={new Date('2023-12-02T17:06:07Z')} changelog="something?\n\nFull changes: https://github.com/ComputerElite/OculusDB/commit/83c8c6e35ff3cb0c78f361b114414cf9a83c8299" />
        <RepoEntry time={new Date('2023-12-02T17:06:07Z')} changelog="something?\n\nFull changes: https://github.com/ComputerElite/OculusDB/commit/83c8c6e35ff3cb0c78f361b114414cf9a83c8299" />
        <RepoEntry time={new Date('2023-12-02T17:06:07Z')} changelog="something?\n\nFull changes: https://github.com/ComputerElite/OculusDB/commit/83c8c6e35ff3cb0c78f361b114414cf9a83c8299" />
        <RepoEntry time={new Date('2023-12-02T17:06:07Z')} changelog="something?\n\nFull changes: https://github.com/ComputerElite/OculusDB/commit/83c8c6e35ff3cb0c78f361b114414cf9a83c8299" />
        <RepoEntry time={new Date('2023-12-02T17:06:07Z')} changelog="something?\n\nFull changes: https://github.com/ComputerElite/OculusDB/commit/83c8c6e35ff3cb0c78f361b114414cf9a83c8299" />
        <RepoEntry time={new Date('2023-12-02T17:06:07Z')} changelog="something?\n\nFull changes: https://github.com/ComputerElite/OculusDB/commit/83c8c6e35ff3cb0c78f361b114414cf9a83c8299" />
        <RepoEntry time={new Date('2023-12-02T17:06:07Z')} changelog="something?\n\nFull changes: https://github.com/ComputerElite/OculusDB/commit/83c8c6e35ff3cb0c78f361b114414cf9a83c8299" />
        <RepoEntry time={new Date('2023-12-02T17:06:07Z')} changelog="something?\n\nFull changes: https://github.com/ComputerElite/OculusDB/commit/83c8c6e35ff3cb0c78f361b114414cf9a83c8299" />
        <RepoEntry time={new Date('2023-12-02T17:06:07Z')} changelog="something?\n\nFull changes: https://github.com/ComputerElite/OculusDB/commit/83c8c6e35ff3cb0c78f361b114414cf9a83c8299" />
        <RepoEntry time={new Date('2023-12-02T17:06:07Z')} changelog="something?\n\nFull changes: https://github.com/ComputerElite/OculusDB/commit/83c8c6e35ff3cb0c78f361b114414cf9a83c8299" />
        <RepoEntry time={new Date('2023-12-02T17:06:07Z')} changelog="something?\n\nFull changes: https://github.com/ComputerElite/OculusDB/commit/83c8c6e35ff3cb0c78f361b114414cf9a83c8299" />
        <RepoEntry time={new Date('2023-12-02T17:06:07Z')} changelog="something?\n\nFull changes: https://github.com/ComputerElite/OculusDB/commit/83c8c6e35ff3cb0c78f361b114414cf9a83c8299" />
      </div>
    </div>
  )
}

export default InfoStats