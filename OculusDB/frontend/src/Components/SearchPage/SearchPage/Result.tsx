import { Match, Switch, Show } from "solid-js"
import ResultData from "../../../Classes/Result"
import limitStringLength from "../../../util/limitStringLength"

class ResultProps{
  app!: ResultData;
  setCurrentTab!: ( tab: string ) => string;
}

const comfortRatingIcons = [
  '/assets/icons/comfortable.svg',
  '/assets/icons/odbicons/moderate.svg',
  '/assets/icons/odbicons/intense.svg',
  '/assets/icons/odbicons/norate.svg',
]

let Result = ( { app, setCurrentTab }: ResultProps ) => {
  return (
    <div class={ app.type !== 'Application' ? "result result-mobile-shrink" : "result" }>
      <Show when={ app.type === 'Application' }>
        <div class="result-icon" style={{ background: 'url(\'https://oculusdb.rui2015.me/cdn/images/' + app.id + '\')' }}></div>
      </Show>

      <div class="result-branding" style={ app.type !== 'Application' ? { width: '100%' } : {} }>
        <div class="result-title">{ limitStringLength(app.name, app.type === 'Application' ? 50 : 100) }</div>
        <div class="result-desc">{ limitStringLength(app.shortDescription,  app.type === 'Application' ? 175 : 250) }</div>

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