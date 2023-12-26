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
]

let Footer = () => {
  return (
    <div class="footer">
      { quotes[Math.floor(Math.random() * (quotes.length - 1))] } | This website is not affiliated with Oculus/Meta VR
    </div>
  )
}

export default Footer