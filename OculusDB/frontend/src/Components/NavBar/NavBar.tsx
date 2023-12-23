import './NavBar.css'

class NavBarProps{
  setCurrentTab!: ( tab: string ) => string;
}

let NavBar = ( props: NavBarProps ) => {
  return (
    <>
      <input type="checkbox" id="nav-open" style={{ display: 'none' }} />
      <div class="navbar">
        <div class="navbar-home" onClick={() => props.setCurrentTab('/home')}>
          <div class="home-icon"></div>
          OculusDB
        </div>

        <label for="nav-open">
          <div class="navbar-dropdown">
            <i class="fa-solid fa-bars"></i>
          </div>
        </label>

        <div class="navbar-buttons">
          <div class="navbar-button" onClick={() => window.open('https://discord.gg/zwRfHQN2UY')}>
            <i class="fa-brands fa-discord"></i>
            <span class="button-label">Discord</span>
          </div>

          <div class="navbar-button" onClick={() => props.setCurrentTab('/supportus')}>
            <span class="button-label">Support Us</span>
          </div>

          <div class="navbar-button" onClick={() => props.setCurrentTab('/guide')}>
            <span class="button-label">Downgrading Guide</span>
          </div>

          <div class="navbar-button" onClick={() => props.setCurrentTab('/stats')}>
            <span class="button-label">Info & Stats</span>
          </div>

          <div class="navbar-button" onClick={() => props.setCurrentTab('/activity')}>
            <span class="button-label">Activity</span>
          </div>

          <div class="navbar-button" onClick={() => props.setCurrentTab('/privacy')}>
            <span class="button-label">Privacy Policy</span>
          </div>

          <div class="navbar-button" onClick={() => props.setCurrentTab('/saved')}>
            <span class="button-label">Saved Apps</span>
          </div>

          <div class="navbar-button" onClick={() => props.setCurrentTab('/login')}>
            <span class="button-label">Login</span>
          </div>

          <div class="navbar-search">
            <input placeholder='Search...' onChange={( el ) => {
              el.currentTarget.value !== '' ? props.setCurrentTab('/search/' + el.currentTarget.value) : null;
              el.currentTarget.value = ''
            }} />
            <i class="fa-solid fa-magnifying-glass"></i>
          </div>
        </div>
      </div>
    </>
  )
}

export default NavBar