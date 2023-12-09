import Search from '../Search/Search';

import { For, onMount, createSignal, createEffect } from 'solid-js';
import './SearchPage.css'
import Result from './SearchPage/Result';
import Application from '../../Classes/Application';

// This will eventually be replaced with a fetch request, to load directly off the server.
const headsets = [
  {
    "headset": 1,
    "codename": "MONTEREY",
    "displayName": "Quest 1",
    "binaryType": 1,
    "group": 0,
    "groupString": "Quest",
    "info": ""
  },
  {
    "headset": 2,
    "codename": "HOLLYWOOD",
    "displayName": "Quest 2",
    "binaryType": 1,
    "group": 0,
    "groupString": "Quest",
    "info": ""
  },
  {
    "headset": 7,
    "codename": "EUREKA",
    "displayName": "Quest 3",
    "binaryType": 1,
    "group": 0,
    "groupString": "Quest",
    "info": ""
  },
  {
    "headset": 6,
    "codename": "SEACLIFF",
    "displayName": "Quest Pro",
    "binaryType": 1,
    "group": 0,
    "groupString": "Quest",
    "info": ""
  },
  {
    "headset": 8,
    "codename": "PANTHER",
    "displayName": "Unknown headset (PANTHER)",
    "binaryType": 1,
    "group": 0,
    "groupString": "Quest",
    "info": ""
  },
  {
    "headset": 0,
    "codename": "RIFT",
    "displayName": "Rift",
    "binaryType": 2,
    "group": 1,
    "groupString": "PCVR",
    "info": "Link compatible"
  },
  {
    "headset": 5,
    "codename": "LAGUNA",
    "displayName": "Rift S",
    "binaryType": 2,
    "group": 1,
    "groupString": "PCVR",
    "info": "Link compatible"
  },
  {
    "headset": 4,
    "codename": "PACIFIC",
    "displayName": "Go",
    "binaryType": 1,
    "group": 2,
    "groupString": "Go",
    "info": ""
  },
  {
    "headset": 3,
    "codename": "GEARVR",
    "displayName": "GearVR",
    "binaryType": 1,
    "group": 3,
    "groupString": "GearVR",
    "info": ""
  }
]

const searchTypes = [ 'Applications', 'DLCs', 'DLC Packs', 'Achievements' ];
let headsetTypes: Array<string> = [];

headsets.forEach(headset => {
  if(headsetTypes.indexOf(headset.groupString) === -1){
    headsetTypes.push(headset.groupString);
  }
})

class SearchPageProps{
  currentTab!: () => string;
  currentSearch!: () => string;
  setCurrentTab!: ( tab: string ) => string;
}

let SearchPage = ( props: SearchPageProps ) => {
  let [ searchType, setSearchType ] = createSignal(0);
  let [ apps, setApps ] = createSignal<Array<Application>>([]);

  let selectorButtons: Array<HTMLElement> = [];
  let headsetButtons: Array<HTMLElement> = [];

  let loadingIndicator: HTMLElement;

  onMount(() => {
    selectorButtons[0].classList.add('button-selected');
  })

  let selectBtn = ( index: number ) => {
    selectorButtons.forEach(btn => {
      btn.classList.remove('button-selected');
    })

    selectorButtons[index].classList.add('button-selected');
    setSearchType(index);
  };

  let headsetFilterToggle = ( ev: MouseEvent, index: number ) => {
    if(ev.shiftKey){
      headsetButtons.forEach(btn => {
        btn.classList.remove('button-selected');
      })

      headsetButtons[index].classList.add('button-selected');
    } else{
      headsetButtons[index].classList.toggle('button-selected');
    }
  }

  createEffect(() => {
    let search = props.currentSearch();
    let type = searchType();

    setApps([]);

    loadingIndicator.style.display = 'flex';
    console.log(search, type);

    fetch('http://localhost:3000/')
      .then(data => data.json())
      .then(data => {
        loadingIndicator.style.display = 'none';
        let tempApps: Array<Application> = [];

        data.forEach(( d: any ) => {
          let app = new Application();
          let addToList = true;

          app.id = d.application.id;
          app.name = d.application.displayName;
          app.comfortRatingFormatted = d.application.comfortRatingFormatted;
          app.shortDescription = d.application.shortDescription;
          app.longDescription = d.application.longDescription;

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

          app.comfortRating = 0;

          switch(app.comfortRatingFormatted){
            case 'Comfortable for most':
              app.comfortRating = 0;
              break;
            case 'Comfortable for some':
              app.comfortRating = 1;
              break;
            case 'Comfortable for few':
              app.comfortRating = 2;
              break;
            default:
              app.comfortRating = 3;
              break;
          }

          if(addToList)
            tempApps.push(app);
          else
            console.log(app);
        })

        setApps(tempApps);
      })
  })

  return (
    <div class="search-page">
      <Search value={ props.currentSearch } setCurrentTab={props.setCurrentTab} />

      <div class="type-filter">
        <For each={searchTypes}>
          {( item, index ) =>  <div class="button" ref={( el ) => selectorButtons.push(el)} onClick={() => selectBtn(index())}>{ item }</div> }
        </For>
      </div>

      <div class="result-columns">
        <div class="more-filters">
          <h2><b>Filters</b></h2>

          <h3>Headsets</h3>
          <p>
            Click to toggle selection.<br />
            Shift + Click to select only the one you clicked on.
          </p>

          <For each={headsetTypes}>
            {( item, index ) =>  <>
              <div class="button button-selected" ref={( el ) => headsetButtons.push(el)} onClick={( e: MouseEvent ) => headsetFilterToggle(e, index())}>
                { item }
              </div>
              <br />
            </> }
          </For>
        </div>

        <div class="results-page">
          <h2 style={{ margin: '10px', 'margin-top': '0', "text-align": 'center' }}>Results for: { props.currentSearch() }</h2>

          <div class="result" ref={( el ) => loadingIndicator = el }>
            <div style={{ width: '100%', "text-align": 'center' }}>
              <span>Searching...</span><br />
              <div class="loader"></div>
            </div>
          </div>

          <For each={apps()}>
            {( item ) => <Result app={item} /> }
          </For>
        </div>
      </div>
    </div>
  )
}

export default SearchPage