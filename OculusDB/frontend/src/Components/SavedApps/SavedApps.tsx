import { Show, For, createSignal, onMount } from 'solid-js';
import './SavedApps.css'
import ResultData from '../../Classes/Result';
import Result from '../SearchPage/SearchPage/Result';

class SavedAppsProps{
  setCurrentTab!: ( tab: string ) => string;
}

let SavedApps = ( props: SavedAppsProps ) => {
  let [ apps, setApps ] = createSignal<ResultData[]>([]);
  let content: HTMLElement;

  let appsToLoad = localStorage.getItem('starred') ? localStorage.getItem('starred')!.split(',') : [];
  let loaded = 0;

  onMount(() => {
    appsToLoad.forEach(app => {
      fetch('https://oculusdb-rewrite.rui2015.me/api/v2/id/' + app)
        .then(data => data.json())
        .then(d => {
          if(d.__OculusDBType !== 'Application')
            return;
  
          let app = new ResultData();
          let addToList = true;
  
          app.shortDescription = d.shortDescription;
          app.longDescription = d.longDescription;
          app.id = d.id;
          app.name = d.displayName;
          app.comfortRatingFormatted = d.comfortRatingFormatted;
          app.comfortRating = d.comfortRating;
          app.type = d.__OculusDBType;
  
          if(!d.offers[0])
            addToList = false;
          else{
            app.priceFormatted = d.offers[0].price.priceFormatted;
            app.rawPrice = d.offers[0].price.price;
  
            if(d.offers[0].strikethroughPrice){
              app.priceOffer = true;
              app.offerPriceFormatted = d.offers[0].price.priceFormatted;
              app.priceFormatted = d.offers[0].strikethroughPrice.priceFormatted;
              app.rawPrice = d.offers[0].strikethroughPrice.price;
            }
          }
  
          if(addToList){
            let a = apps();
  
            a.push(app);
            setApps(a);
          }

          loaded++;

          if(loaded == appsToLoad.length){
            content.appendChild(<div>
              <For each={apps()}>
                {( app ) => <div>
                  <Result app={app} setCurrentTab={props.setCurrentTab} />
                </div>}
              </For>
            </div> as Node)
          }
        })
    })
  })

  return (
    <div>
      <div class="main">
        <div class="info">
          <Show when={appsToLoad.length === 0}>
            <p style={{ "text-align": 'center' }}>You haven't saved any apps on this web browser.</p>
          </Show>

          <div ref={( el ) => content = el}></div>
        </div>
      </div>
    </div>
  )
}

export default SavedApps;