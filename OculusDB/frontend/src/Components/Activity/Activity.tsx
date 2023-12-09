import ActivityInfo from './Activity/ActivityInfo';

import './Activity.css'

let Activity = () => {
  return (
    <div class="main">
      <div class="info" style={{ "text-align": 'center' }}>
        <h1><b>Recent Activity</b></h1>

        <h2>Want to get these updates on your Discord server?</h2>
        <p>
          Hit me up on Discord <b>computerelite</b>. I will add you once you messaged me.
        </p>

        <div class="activity-options">
          <input type="checkbox" id="show-updates-toggle" checked={true} style={{ display: 'none' }} />

          <label for="show-updates-toggle">
            <div class="button">
              Show Updates
            </div>
          </label>


          <input type="checkbox" id="show-apps-toggle" checked={true} style={{ display: 'none' }} />

          <label for="show-apps-toggle">
            <div class="button">
              Show Applications
            </div>
          </label>


          <input type="checkbox" id="show-price-toggle" checked={true} style={{ display: 'none' }} />

          <label for="show-price-toggle">
            <div class="button">
              Show Price Changes
            </div>
          </label>


          <input type="checkbox" id="show-dlc-toggle" checked={true} style={{ display: 'none' }} />

          <label for="show-dlc-toggle">
            <div class="button">
              Show DLCs
            </div>
          </label>
        </div>

        <div class="activities">
          <ActivityInfo />
          <ActivityInfo />
          <ActivityInfo />
          <ActivityInfo />
          <ActivityInfo />
          <ActivityInfo />
          <ActivityInfo />
          <ActivityInfo />
        </div>
      </div>
    </div>
  )
}

export default Activity