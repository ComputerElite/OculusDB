import Search from '../Search/Search'

import './Home.css'

class HomeProps{
  setCurrentTab!: ( tab: string ) => string;
}

let Home = ( props: HomeProps ) => {
  return (
    <div class="main">
      <Search setCurrentTab={props.setCurrentTab} />

      <div class="info">
        <h1>Welcome to Oculus DB</h1>

        <h3><i>An Oculus exploring and monitoring service</i></h3>
        <p>OculusDB monitors the entirety of Oculus with it's own database which gets regularly updated. </p>

        <h2>Database info</h2>
        <p>
          All dates you'll see on this site will be in your timezone.<br />
          All data is being gathered from Oculus GraphQL api at <a href="https://graph.oculus.com/graphql">https://graph.oculus.com/graphql</a>.
        </p>

        <h2>Download info</h2>

        <p><b>Downloads</b> may <b>only work</b> if you are <b>logged in on <a href="https://developer.oculus.com/manage/">https://developer.oculus.com/manage/</a></b> with your Oculus Account which owns the game you want to download.</p>

        <h2>Project info</h2>
        <p>
        OculusDB is always in development. You can find the source code on <a href="https://github.com/ComputerElite/OculusDB">GitHub</a>.<br />
        If you got any requests for what to add to OculusDB or feedback about the website itself feel free to tell me via <a href="https://github.com/ComputerElite/OculusDB/issues">GitHub Issues</a> or the <a href="https://discord.gg/zwRfHQN2UY">OculusDB Discord Server</a>.<br />
        If you have experience in designing pictograms or logos I'd be happy if you hit me up on Discord as I want to add a few pictograms here and there. To message me join the <a href="https://discord.gg/zwRfHQN2UY">OculusDB Discord Server</a>.
        </p>

        <h2>API</h2>

        <p>OculusDB has an api anyone is allowed to use. You can find it <a href="https://oculusdb.rui2015.me/api/docs">here</a>.</p>
      </div>
    </div>
  )
}

export default Home