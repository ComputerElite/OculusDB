/* @refresh reload */
import { render } from 'solid-js/web'
import Lenis from '@studio-freight/lenis'

import './index.css'
import App from './Components/App/App'

const root = document.getElementById('root')

render(() => <div><App /></div>, root!)

const lenis = new Lenis({
  wrapper: root!,
  content: root!.firstChild! as HTMLElement
});

let raf = ( time: number ) => {
  lenis.raf(time);
  requestAnimationFrame(raf);
}

requestAnimationFrame(raf);