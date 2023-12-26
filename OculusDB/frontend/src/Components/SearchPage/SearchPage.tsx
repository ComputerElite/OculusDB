import Search from '../Search/Search';

import { For, onMount, createSignal, createEffect } from 'solid-js';
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
  let [ searchHeadset, setSearchHeadset ] = createSignal("Quest,PCVR,GoAndGearVr");
  let [ apps, setApps ] = createSignal<Array<ResultData>>([]);

  let selectorButtons: Array<HTMLElement> = [];
  let headsetButtons: Array<HTMLElement> = [];

  let loadingIndicator: HTMLElement;

  let filterTypesEl: HTMLElement;
  let searchTypesEl: HTMLElement;

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

          <select class="currency-selection">
            <option>Currency 1</option>
            <option>Currency 2</option>
            <option>Currency 3</option>
          </select>
        </div> as Node)

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

    console.log(`https://oculusdb-rewrite.rui2015.me/api/v2/search?q=${encodeURIComponent(search)}&type=${searchTypes[type].enumName}&groups=${headset}`);
    fetch(`https://oculusdb-rewrite.rui2015.me/api/v2/search?q=${encodeURIComponent(search)}&type=${searchTypes[type].enumName}&groups=${headset}`)
      .then(data => data.json())
      .then(data => {
        loadingIndicator.style.display = 'none';
        let tempApps: Array<ResultData> = [];

        data.results.forEach(( d: any ) => {
          // console.log(d);
          let app = new ResultData();
          let addToList = true;

          switch(d.__OculusDBType){
            case 'Application':
              app.shortDescription = d.shortDescription;
              app.longDescription = d.longDescription;
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

          if(addToList)
            tempApps.push(app);
          // else
          //   console.log(false);
        })

        setApps(tempApps);
      })
  })

  return (
    <div class="search-page">
      <Search value={ props.currentSearch } setCurrentTab={props.setCurrentTab} />

      <div class="type-filter" ref={( el ) => searchTypesEl = el}>
        <For each={searchTypes}>
          {( item, index ) =>  <div class="button" ref={( el ) => selectorButtons.push(el)} onClick={() => selectBtn(index())}>{ item.displayName }</div> }
        </For>

        <select class="currency-selection">
          <option>Currency 1</option>
          <option>Currency 2</option>
          <option>Currency 3</option>
        </select>
      </div>

      <div class="result-columns">
        <div class="more-filters">
          <div class="more-filter-headers">
            <h2><b>Filters</b></h2>

            <div>
              <h3>Headsets</h3>
              <p>
                Click to toggle selection.<br />
                Shift + Click to select only the one you clicked on.
              </p>
            </div>
          </div>

          <div class="headset-selection-list" ref={( el ) => filterTypesEl = el}></div>
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