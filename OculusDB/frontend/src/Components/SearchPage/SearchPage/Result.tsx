import { Match, Switch } from "solid-js"
import Application from "../../../Classes/Application"
import limitStringLength from "../../../util/limitStringLength"

class ResultProps{
  app!: Application;
  setCurrentTab!: ( tab: string ) => string;
}

const comfortRatingIcons = [
  'https://cdn.phazed.xyz/odbicons/comfortable.svg',
  'https://cdn.phazed.xyz/odbicons/moderate.svg',
  'https://cdn.phazed.xyz/odbicons/intense.svg',
  'https://cdn.phazed.xyz/odbicons/norate.svg',
]

let Result = ( { app, setCurrentTab }: ResultProps ) => {
  return (
    <div class="result">
    <div class="result-icon" style={{ background: 'url(\'https://oculusdb.rui2015.me/cdn/images/' + app.id + '\')' }}></div>
      <div class="result-branding">
        <div class="result-title">{ limitStringLength(app.name, 50) }</div>
        <div class="result-desc">{ limitStringLength(app.shortDescription, 175) }</div>

        <div class="rating-format">
          <img width="20" src={ comfortRatingIcons[app.comfortRating] } />
          <div style={{ "margin-left": '10px' }}>{ app.comfortRatingFormatted }</div>
        </div>

        <div class="result-price">
          <Switch>
            <Match when={app.rawPrice === 0}>
              <span style={{ color: '#63fac3' }}>Free</span>
            </Match>
            <Match when={app.priceOffer}>
              <span style={{ "text-decoration": 'line-through', color: '#aaa' }}>{ app.offerPriceFormatted }</span>
              <br />
              <span style={{ color: '#63fac3' }}>{ app.priceFormatted }</span>
            </Match>
            <Match when={!app.priceOffer}>
              <span style={{ color: '#63fac3' }}>{ app.priceFormatted }</span>
            </Match>
          </Switch>
        </div>
        <div class="button" onClick={() => setCurrentTab('/id/'+app.id)}>Details</div>
      </div>
    </div>
  )
}

export default Result