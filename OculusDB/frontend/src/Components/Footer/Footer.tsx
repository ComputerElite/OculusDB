import './Footer.css'

const quotes = [
  "\"We don't have to be efficient\" - Computer",
  "\"Oh Yes\" - Computer",
  "\"you just gotta solder a new one on your ass\" - Computer",
  "\"Sales pitch: It can hit people\" - Computer",
  "\"help, I have arms now\" - Computer",
  "\"He's having a bad dream\" - Computer",
  "\"I didn't talk about being tied to a bed\" - Computer",
  "\"QuestAppVersionSwither\" - Computer",
  "\"Shut the fuck off\" - Computer",
  "\"xD\" - Phaze",
  "\"fucking hell computer\" - Phaze",
  "\"you've basically got admin access to everything\" - Phaze",
  "\"that's half gay duh\" - Computer",
  "\"the gay, its taking me\" - Phaze",
  "\"that's like using a nuke to amputate your foot\" - Phaze",
  "\"SCOTLAND FOREEEVER\" - ComputerElite",
  "\"SCOOOOOOOOOOOOTLAND\" - Phaze",
  "\"what is that? Selfie stick base station?\" - Computer",
  "\"I FOUND IT\" - Phaze",
  "\"I'm better than Meta\" - Computer (according to a classmate)",
  "\"WE NEED MORE QUOTES\" - Computer",
  "\"see, i'm on top now\" - Computer"
]

let Footer = () => {
  return (
    <div class="footer">
      { quotes[Math.floor(Math.random() * (quotes.length - 1))] } | This website is not affiliated with Oculus/Meta VR
    </div>
  )
}

export default Footer
