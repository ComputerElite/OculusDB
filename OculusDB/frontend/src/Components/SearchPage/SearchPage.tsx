import Search from '../Search/Search';

import { For, onMount, createSignal, createEffect, Show } from 'solid-js';
import './SearchPage.css'
import Result from './SearchPage/Result';
import ResultData from '../../Classes/Result';

let headsets: Array<any> = []

let searchTypes: Array<any> = [];
let headsetTypes: Array<{ group: string, name: string }> = [];

class SearchPageProps{
  currentTab!: () => string;
  currentSearch!: () => string;
  setCurrentTab!: ( tab: string ) => string;
  query!: () => any;
  setQuery!: ( query: any ) => any;
}

let SearchPage = ( props: SearchPageProps ) => {
  let [ searchType, setSearchType ] = createSignal(-1);
  let [ searchHeadset, setSearchHeadset ] = createSignal(props.query().group || 'Quest,PCVR,GoAndGearVr');
  let [ apps, setApps ] = createSignal<Array<ResultData>>([]);

  let selectorButtons: Array<HTMLElement> = [];
  let headsetButtons: Array<HTMLElement> = [];
  let languageButtons: Array<HTMLElement> = [];

  let loadingIndicator: HTMLElement;

  let filterTypesEl: HTMLElement;
  let languageFilterEl: HTMLElement;
  let searchTypesEl: HTMLElement;

  let currencySelector: HTMLSelectElement;
  let availableCurrencies: string[] = [];

  onMount(() => {
    fetch('https://oculusdb-rewrite.rui2015.me/api/v2/lists/headsets')
      .then(data => data.json())
      .then(data => {
        headsets = data;
        headsetTypes = [];

        headsets.forEach(headset => {
          let type = headsetTypes.find(x => x.group === headset.groupString);

          if(type){
            type.name += ', ' + headset.displayName;
          } else{
            headsetTypes.push({ group: headset.groupString, name: headset.displayName });
          }
        })

        let selectedHeadsetGroups = props.query().group ? props.query().group.split(',') : headsetTypes.map(x => x.group);

        filterTypesEl.innerHTML = '';
        filterTypesEl.appendChild((
          <div>
            <For each={headsetTypes}>
              {( item, index ) =>  <>
                <div class={ selectedHeadsetGroups.indexOf(item.group) !== -1 ? "button button-selected" : "button" } ref={( el ) => headsetButtons.push(el)} onClick={( e: MouseEvent ) => headsetFilterToggle(e, index())}>
                  { item.name }
                </div>
                <br />
              </> }
            </For>
          </div>
        ) as Node)
      })

    fetch('https://oculusdb-rewrite.rui2015.me/api/v2/lists/searchcategories')
      .then(data => data.json())
      .then(data => {
        searchTypes = data;

        searchTypesEl.innerHTML = '';
        searchTypesEl.appendChild(<div>
          <For each={searchTypes}>
            {( item, index ) =>  <div class="button" ref={( el ) => selectorButtons.push(el)} onClick={() => selectBtn(index())}>{ item.displayName }</div> }
          </For>

          <select ref={( el ) => currencySelector = el} class="currency-selection"></select>
        </div> as Node);

        selectorButtons[props.query().type || 0].classList.add('button-selected');
        setSearchType(props.query().type || 0);
      });
  })

  let selectBtn = ( index: number ) => {
    selectorButtons.forEach(btn => {
      btn.classList.remove('button-selected');
    })

    selectorButtons[index].classList.add('button-selected');
    setSearchType(index);

    let q = props.query();

    q['type'] = index;
    props.setQuery(q);
  };

  let headsetFilterToggle = ( ev: MouseEvent, index: number ) => {
    if(ev.shiftKey){
      headsetButtons.forEach(btn => {
        btn.classList.remove('button-selected');
      })

      headsetButtons[index].classList.add('button-selected');
      setSearchHeadset(headsetTypes[index].group);

      let q = props.query();

      q['group'] = headsetTypes[index].group;
      props.setQuery(q);
    } else{
      headsetButtons[index].classList.toggle('button-selected');

      let headsetType: Array<string> = [];
      headsetButtons.forEach((btn, i) => {
        if(btn.classList.contains('button-selected'))
          headsetType.push(headsetTypes[i].group);
      })

      setSearchHeadset(headsetType.join(','));

      let q = props.query();

      q['group'] = headsetType.join(',');
      props.setQuery(q);
    }
  }

  createEffect(() => {
    let search = props.currentSearch();
    let type = searchType();
    let headset = searchHeadset();

    if(type === -1)
      return;

    setApps([]);
    loadingIndicator.style.display = 'flex';

    console.log(headset);

    console.log(`https://oculusdb-rewrite.rui2015.me/api/v2/search?q=${encodeURIComponent(search)}&type=${searchTypes[type].enumName}&groups=${headset}`);
    fetch(`https://oculusdb-rewrite.rui2015.me/api/v2/search?q=${encodeURIComponent(search)}&type=${searchTypes[type].enumName}&groups=${headset}`)
      .then(data => data.json())
      .then(data => {
        console.log(data);
        loadingIndicator.style.display = 'none';

        let languageFilterToggle = ( ev: MouseEvent, index: number ) => {
          if(ev.shiftKey){
            languageButtons.forEach(btn => {
              btn.classList.remove('button-selected');
            })
      
            languageButtons[index].classList.add('button-selected');
      
            let q = props.query();
      
            q['language'] = languageButtons[index].innerText;
            props.setQuery(q);

            processResults();
          } else{
            languageButtons[index].classList.toggle('button-selected');
      
            let languageType: Array<string> = [];
            

            languageButtons.forEach((btn, i) => {
              if(btn.classList.contains('button-selected'))
                languageType.push(languageButtons[i].innerText);
            })
      
            let q = props.query();
      
            q['language'] = languageType.join(',');
            props.setQuery(q);

            processResults();
          }
        }

        let processResults = () => {
          let tempApps: Array<ResultData> = [];
          let languages: Array<string> = [];

          data.results.forEach(( d: any ) => {
            let app = new ResultData();
            let addToList = true;

            switch(d.__OculusDBType){
              case 'Application':
                app.shortDescription = d.shortDescription;
                app.longDescription = d.longDescription;
                app.languages = d.supportedInAppLanguages.map(( x: string ) => x.toUpperCase());

                d.supportedInAppLanguages.forEach(( lan: string ) => {
                  if(!languages.find(x => x === lan.split('_')[0].toUpperCase())){
                    languages.push(lan.split('_')[0].toUpperCase());
                  }
                })    
                break;

              case 'IapItemPack':
                app.shortDescription = d.displayShortDescription;
                app.longDescription = d.displayShortDescription;
                break;

              case 'IapItem':
                app.shortDescription = d.displayShortDescription;
                app.longDescription = d.displayShortDescription;
                break;
            }

            app.id = d.id;
            app.name = d.displayName;
            app.comfortRatingFormatted = d.comfortRatingFormatted;
            app.comfortRating = d.comfortRating;
            app.type = d.__OculusDBType;
            app.groupFormatted = d.groupFormatted;

            d.offers.forEach(( o: any ) => {
              if(!availableCurrencies.find(x => x === o.currency)){
                availableCurrencies.push(o.currency);
                currencySelector.appendChild(<option>{ o.currency }</option> as Node);
                console.log(currencySelector);
              }
            })

            let canUseSelectedOffer = true;
            let selectedOffer = d.offers.find(( x: any ) => x.currency === localStorage.getItem('currency'));

            if(!selectedOffer){
              selectedOffer = d.offers[0];
              canUseSelectedOffer = false;
            }

            if(!selectedOffer)
              addToList = false;
            else{
              app.priceIsSelected = canUseSelectedOffer
              app.priceFormatted = selectedOffer.price.priceFormatted;
              app.rawPrice = selectedOffer.price.price;

              if(selectedOffer.strikethroughPrice){
                app.priceOffer = true;
                app.offerPriceFormatted = selectedOffer.price.priceFormatted;
                app.priceFormatted = selectedOffer.strikethroughPrice.priceFormatted;
                app.rawPrice = selectedOffer.strikethroughPrice.price;
              }
            }

            if(addToList)
              tempApps.push(app);
            // else
            //   console.log(false);
          })

          let langs = props.query().language ? props.query().language.split(',') : languages;

          tempApps = tempApps.filter(x => {
            if(x.type !== 'Application')
              return true;

            let hasLang = false;

            x.languages.forEach(lan => {
              if(langs.find(( x: string ) => x === lan))
                hasLang = true;
            })

            return hasLang;
          })

          setApps(tempApps);

          languageFilterEl.innerHTML = '';
          languageButtons = [];

          languages.forEach((lang, i) => {
            languageFilterEl.appendChild(
              <div class={langs.find((x: string) => x === lang) ? "button button-selected" : "button"} ref={( el ) => languageButtons.push(el)} style={{ width: '30px' }} onClick={( e: MouseEvent ) => languageFilterToggle(e, i)}>
                {lang}
              </div> as Node
            )
          })
        }

        processResults();
        currencySelector.value = localStorage.getItem('currency') || 'EUR';

        currencySelector.onchange = () => {
          localStorage.setItem('currency', currencySelector.value);
          processResults();
        }
      })
  })

  return (
    <div class="search-page">
      <Search value={ props.currentSearch } setCurrentTab={props.setCurrentTab} />

      <div class="type-filter" ref={( el ) => searchTypesEl = el}>
        <For each={searchTypes}>
          {( item, index ) =>  <div class="button" ref={( el ) => selectorButtons.push(el)} onClick={() => selectBtn(index())}>{ item.displayName }</div> }
        </For>
      </div>

      <div class="result-columns">
        <div class="more-filters">
          <div class="more-filter-headers">
            <h2><b>Filters</b></h2>

            <p>
              Click to toggle selection.<br />
              Shift + Click to select only the one you clicked on.
            </p>

            <div>
              <h3>Headsets</h3>
            </div>
          </div>

          <div class="headset-selection-list" ref={( el ) => filterTypesEl = el}></div><br />

          <Show when={searchType() === 0}>
            <hr />
            <div>
              <h3>App Languages</h3>
            </div>

            <div ref={( el ) => languageFilterEl = el}>Loading...</div>
          </Show>
        </div>

        <div class="results-page">
          <h2 style={{ margin: '10px', 'margin-top': '0', "text-align": 'center' }}>Results for: { props.currentSearch() }</h2>

          <div class="result" ref={( el ) => loadingIndicator = el } style={{ "align-items": 'center' }}>
            <div style={{ width: '100%', "text-align": 'center', height: 'fit-content' }}>
              <span>Searching...</span><br />
              <div class="loader"></div>
            </div>
          </div>

          <For each={apps()}>
            {( item ) => <Result app={item} setCurrentTab={props.setCurrentTab} /> }
          </For>
        </div>
      </div>
    </div>
  )
}

export default SearchPage