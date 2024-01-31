import './DowngradingGuide.css'

class DowngradingGuideProps{
  setCurrentTab!: ( tab: string ) => string;
}

let DowngradingGuide = ( props: DowngradingGuideProps) => {
  return (
    <div class="main">
      <div class="info">
        <h1><b>Downgrading Guide</b></h1>

        <h2>Which headset do you use?</h2>

        <div class="button" onClick={() => props.setCurrentTab('/guide/quest')}>Quest</div>
        <div class="button" onClick={() => props.setCurrentTab('/guide/rift')}>Rift / Oculus Link</div>
      </div>
    </div>
  )
}

let DowngradingGuideRift = ( props: DowngradingGuideProps ) => {
  return (
    <div class="main">
      <div class="info">
        <h1>Downgrading rift games</h1>

        <h2>1. Search the game you want to downgrade</h2>
        <p>
          Search the game in the search box at the top right corner.<br />
          Click the details button on the game. Make sure that the game is for your headset (Rift and Rift S apply for Link and Air Link as well)
        </p>

        <h2>2. Download the desired version</h2>
        <p>
          Scroll down and click on <b>Versions</b>. Then click the download button on the version you want and follow the provided instructions
        </p>

        <h2>Congrats. You now downgraded your game</h2>
        <p>If you need help join the <a href="https://discord.gg/zwRfHQN2UY">OculusDB Discord server</a> and ask for help there.</p>

        <div class="button" onClick={() => props.setCurrentTab('/guide')}>Back</div>
      </div>
    </div>
  )
}

let DowngradingGuideQuest = ( props: DowngradingGuideProps ) => {
  return (
    <div class="main">
      <div class="info">
        <h1><b>Downgrading Guide</b></h1>

        <h2>Do you have a PC with Sidequest (press no if you want to mod on android)?</h2>

        <div class="button" onClick={() => props.setCurrentTab('/guide/quest/sqq')}>Yes</div>
        <div class="button" onClick={() => props.setCurrentTab('/guide/quest/qavs')}>No</div>
        <div class="button" onClick={() => props.setCurrentTab('/guide')}>Back</div>
      </div>
    </div>
  )
}

let DowngradingGuideQuestSqq = ( props: DowngradingGuideProps ) => {
  return (
    <div class="main">
      <div class="info">
        <h1><b>Downgrading Guide</b></h1>

        <h2>Does your SideQuest look like this?</h2>

        <img src="/assets/oculusdbsq.png" /><br />

        <div class="button" onClick={() => props.setCurrentTab('/guide/quest/qavs')}>Yes</div>
        <div class="button" onClick={() => props.setCurrentTab('/guide/quest/pc')}>No</div>
        <div class="button" onClick={() => props.setCurrentTab('/guide/quest')}>Back</div>
      </div>
    </div>
  )
}

let DowngradingGuideQuestQavs = ( props: DowngradingGuideProps ) => {
  return (
    <div class="main">
      <div class="info">
        <h1><b>Downgrading Guide</b></h1>

        <h2>Video Tutorial by VR Generation</h2>

        <iframe src="https://www.youtube.com/embed/XE2o24_yack"></iframe><br />

        <h3>If you need help, join the <a href="https://discord.gg/zwRfHQN2UY">OculusDB Discord Server</a> and ask your question in <b>#support</b></h3><br />

        <h2>1. Install Sidequest</h2>
        <p>Follow <a href="https://sidequestvr.com/setup-howto">this guide</a> install SideQuest to your vr headset or get SideQuest from the PlayStore if you are on an android phone.</p>

        <h2>2. Install QuestAppVersionSwitcher</h2>
        <p>After setting up SideQuest search for <b>QuestAppVersionSwitcher</b> and install it to your headset. </p>

        <h2>3. Set up QuestAppVersionSwitcher</h2>
        <p>
          In order to downgrade games you have to log in to your Meta account on QuestAppVersionSwitcher. To do that open QuestAppVersionSwitcher (from the unknown sources menu in your Quest) and allow the storage permission.<br />
          Open the <b>Tools & Options</b> tab in QuestAppVersionSwitcher and press the Login button. Log in to your Meta account like you would normally.
        </p>

        <h2>QuestAppVersionSwitcher is white when you open it?</h2>
        <p>Try clicking on the white and allow storage permission if a prompt appears. If it doesn't work restart your Quest and try again.</p>

        <h2>Stuck in a login loop?</h2>
        <p>Open QuestAppVersionSwitcher on your PC, Laptop or phone by opening the link shown at the bottom of the <b>Tools & Options</b> tab on your PC, Laptop or phone. Follow <a href="https://computerelite.github.io/tools/Oculus/ObtainToken.html">this guide</a> to copy your token and past it into the <b>Token</b> box in the <b>Tools & Options</b> tab and hit <b>Login with token</b>.</p>

        <h2>4. Download the downgraded game</h2>
        <p>After you logged in go to the <b>Downgrade</b> tab in QuestAppVersionSwitcher. Search for the game you want to downgrade and press the Download button on a version you want. The download should start and you'll see the progress of the download in the <b>Download progress</b> tab.</p>

        <h2>5. (Optional) Back up your app data (saves, scores, settings, ...)</h2>
        <p>
          If you want to back up your scores head to the <b>Backup</b> tab in QuestAppVersionSwitcher and select your app by pressing the <b>Change app</b> button (if you are unsure what to select, search the game below step 6.1).<br />
          At the bottom type in a backup name (e. g. <b>my_backup</b>), check <b>Only backup app data</b> and press the <b>Create Backup</b> button.<br />
          You can then restore the app data by selecting it in the list above and pressing restore
        </p>

        <h2>6. Install the Downgraded game</h2>
        <p>From the list of backups select the version you just downloaded. Click the <b>Restore button</b> and follow the provided steps.</p>

        <h2>7. Have fun</h2>
        <p>You can now play the downgraded game. If you need help join the <a href="https://discord.gg/zwRfHQN2UY">OculusDB Discord server</a> and ask for help there. </p>

        <div class="button" onClick={() => props.setCurrentTab('/guide/quest')}>Back</div>
      </div>
    </div>
  )
}

let DowngradingGuidePc = ( props: DowngradingGuideProps ) => {
  return (
    <div class="main">
      <div class="info">
        <h1><b>Downgrading games</b></h1>
        <p>This page is not finished</p><br />

        <h2>1. Type in the games name</h2>
        <input placeholder='Enter Game Name...' />
        <select>
          <option>Quest</option>
          <option>PCVR</option>
          <option>Go</option>
          <option>GearVR</option>
        </select>

        <h2>2. Type in the version you want</h2>
        <input placeholder='Enter Game Version...' />

        <h2>3. Hit Search</h2>
        <div class="button">Search</div>

        <h2>Result</h2>

        <div>
          <p>Results will appear after you search.</p>
        </div>

        <h2>4. Download and install the downloaded apk</h2>
        <p>
          After pressing the download button and doing what it says Uninstall the game (9 squares on the top right of sidequest, click the cog on the game and press uninstall app) and afterwards drag and drop the file you downloaded from the download button above into SideQuest to install the apk.
        </p>

        <h2>Congrats. You now downgraded your game</h2>

        <input type="checkbox" id="help-dropdown-1" style={{ display: 'none' }} />
        <div class="dropdown">
          <label for="help-dropdown-1">
            <div class="dropdown-heading">
              <p>
                <i class="fa-solid fa-circle-arrow-right"></i> Help: The APK fails to install on Sidequest and says <b>SAFESIDE</b> or is stuck on <b>Checking APK against blacklist</b>
              </p>
            </div>
          </label>

          <div class="dropdown-contents">
            Download <b>MultiUserADBInterface.zip</b> from <a href="https://github.com/ComputerElite/MultiUserADBInterface/releases/latest">here</a>. Then unzip the file you downloaded (right click -&gt; extract all). After that's done open the MultiUserADBInterface executable and type in <a>1</a> followed by enter to install an apk. Then drag and drop the apk on which you got the error when installing with Sidequest into the window and press enter. Finally select the User you want to install the apk and you're done.<br /><br />

            <i>do all of this while your quest is plugged in. If you have any issues feel free to ask</i><br /><br />

            Note: This will <b>not</b> make Sidequest able to install the APK. It'll install the APK without Sidequest.
          </div>
        </div>

        <p>If you need help join the <a href="https://discord.gg/zwRfHQN2UY">OculusDB Discord server</a> and ask for help there. </p>

        <div class="button" onClick={() => props.setCurrentTab('/guide/quest/sqq')}>Back</div>
      </div>
    </div>
  )
}

export { DowngradingGuide, DowngradingGuideQuest, DowngradingGuideQuestSqq, DowngradingGuideQuestQavs, DowngradingGuideRift, DowngradingGuidePc }