import './PrivacyPolicy.css'

let PrivacyPolicy = () => {
  return (
    <div class="main">
      <div class="info">
        <h1>Privacy policy</h1>

        <h2>General</h2>
        <p>This document will tell you which data may get collected when you visit and use this site.</p>

        <h2>Logging by this site</h2>
        <p>
          This page is self-hosted on <a href="https://www.oracle.com/legal/privacy/privacy-policy.html">Oracle</a>. Following data may be collected by accessing it:<br /><br />

          - Your IP address<br />
          - The browser you're using<br />
          - The site you're coming From<br />
          - The time you visited this site<br />
          - Visited pages<br />
          - The operating system you use<br />
        </p>

        <h2>Logging by ComputerAnalytics</h2>
        <p>
          ComputerAnalytics is an analytics tools which provides information such as:<br /><br />

          - Amount of clicks on the website<br />
          - Where visitors came from<br />
          - How long they were on the page and the time they visited it<br />
          - The page you visited<br />
          - Your devices screen height and width (which may or may not be accurate)<br /><br />

          ComputerAnalytics itself is open source and it's code can be viewed by everyone <a href="">here</a><br />
          How ComputerAnalytics works:<br /><br />

          - It records the things mentioned above.<br />
          - After you leave the page the data gets sent to ComputerAnalytics main server at <a href="https://analytics.rui2015.me/">https://analytics.rui2015.me/</a><br />
          - The data will not be associated with you<br /><br />

          ComputerAnalytics privacy policy can be found <a href="https://analytics.rui2015.me/privacy">here</a>
          A writeup containing information about how ComputerAnalytics works can be found <a href="https://github.com/ComputerElite/ComputerAnalytics/wiki">here</a>
          <b>The data is not being merged with any other data of you</b>
        </p>

        <h2>Cookies</h2>
        <p>
          OculusDB <b>only</b> uses cookies if you have logged in as an administrator so the server can authenticate you.<br />
          ComputerAnalytics uses cookies to give you an unique identifier which anonymises you in its database.
        </p>

        <h2>Additional information</h2>
        <p>
          The workings of the site can be checked any time as the source code is publicly available at <a href="https://github.com/ComputerElite/OculusDB">https://github.com/ComputerElite/OculusDB</a>
        </p>
      </div>
    </div>
  )
}

export default PrivacyPolicy