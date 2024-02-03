import { onMount } from 'solid-js';
import './Footer.css'

let Footer = () => {
  let footer: HTMLElement;

  let quotes = [
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
    "\"see, i'm on top now\" - Computer",
    "\"He didn't want to cum. Well, come, not cum. You know what I mean duh.\" - Computer",
    "\"kiss me phaze\" - Computer",
    "\"Phaz, come here, You're gonna put on the collar\" - Computer",
    "\"Currency 1 is my FAVOURITE currency\" - Computer",
    "\"broken? you never implemented it\" - Computer",
    "\"love you\" - Phaze to Computer",
    "Never gonna give you up, never gonna let you down, never gonna run around and desert you.",
    "Quotes are guaranteed to be out of context in more than 1 occurrance",
    "\"wrong! I am not a furry\" - Phaze",
    "\"Idk if I'm legally allowed to...\" - Computer to Phaze",
    "\"well that was the lazy route lmao\" - Computer to Phaze",
    "\"can I see?\" - Computer to Phaze",
    "\"Thanks phaz 😘\" - Computer to Phaze",
    "\"You will never make the Popup good enough. Users are too stupid\" - Computer to John",
    "\"i'll add quotes everywhere, everything shall be a quote.\" - Phaze",
    "\"Add that as a quote\" - Computer",
    "\"💀\" - Computer",
    "\"why finish the ui when i can add more quotes?\" - Phaze",
    "\"JOHN! UNFORK THIS REPO RIGHT FUCKING NOW\" - Phase",
    "\"Certified phaze moment. Proceeds to fuck...\" - John",
    "\"john. merge fix your conflicts\" - Phaze",
    "\"TAKE YOUR CLOTHES OFF PHAZE\" - Computer"
  ]

  let randomQuote = () => {
    let newQuotes = quotes;

    let qs = localStorage.getItem('recent-quotes')?.split(',');
    if(qs){
      qs.forEach(q => {
        newQuotes.splice(parseInt(q), 1);
      })
    } else
      qs = [];

    let q = newQuotes[Math.floor(Math.random() * (newQuotes.length - 1))];
    qs.push(quotes.indexOf(q).toString());

    localStorage.setItem('recent-quotes', qs.slice(-5).join(','));
    return q;
  }

  onMount(() => {
    footer.innerHTML = randomQuote() + ' | This website is not affiliated with Oculus/Meta VR'
  })

  return (
    <div class="footer" ref={( el ) => footer = el}>
      Loading... | This website is not affiliated with Oculus/Meta VR
    </div>
  )
}

export default Footer
