import { onMount } from 'solid-js';
import './Footer.css'

let Footer = () => {
  let footer: HTMLElement;

  onMount(() => {
    fetch('https://cdn.phazed.xyz/odbquotes.json')
      .then(data => data.json())
      .then(quotes => {
        footer.innerHTML = quotes[Math.floor(Math.random() * (quotes.length - 1))] + ' | This website is not affiliated with Oculus/Meta VR'
      })
  })

  return (
    <div class="footer" ref={( el ) => footer = el}>
      Loading... | This website is not affiliated with Oculus/Meta VR
    </div>
  )
}

export default Footer
