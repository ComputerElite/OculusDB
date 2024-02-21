import Search from '../Search/Search';

import { For, onMount, createSignal, createEffect } from 'solid-js';
import './SearchPage.css'
import Result from './SearchPage/Result';
import ResultData from '../../Classes/Result';

let headsets: Array<any> = []

let searchTypes: Array<any> = [];
let headsetTypes: Array<{ group: string, name: string }> = [];

let countries = [{"Code": "AF", "Name": "Afghanistan"},{"Code": "AX", "Name": "\u00c5land Islands"},{"Code": "AL", "Name": "Albania"},{"Code": "DZ", "Name": "Algeria"},{"Code": "AS", "Name": "American Samoa"},{"Code": "AD", "Name": "Andorra"},{"Code": "AO", "Name": "Angola"},{"Code": "AI", "Name": "Anguilla"},{"Code": "AQ", "Name": "Antarctica"},{"Code": "AG", "Name": "Antigua and Barbuda"},{"Code": "AR", "Name": "Argentina"},{"Code": "AM", "Name": "Armenia"},{"Code": "AW", "Name": "Aruba"},{"Code": "AU", "Name": "Australia"},{"Code": "AT", "Name": "Austria"},{"Code": "AZ", "Name": "Azerbaijan"},{"Code": "BS", "Name": "Bahamas"},{"Code": "BH", "Name": "Bahrain"},{"Code": "BD", "Name": "Bangladesh"},{"Code": "BB", "Name": "Barbados"},{"Code": "BY", "Name": "Belarus"},{"Code": "BE", "Name": "Belgium"},{"Code": "BZ", "Name": "Belize"},{"Code": "BJ", "Name": "Benin"},{"Code": "BM", "Name": "Bermuda"},{"Code": "BT", "Name": "Bhutan"},{"Code": "BO", "Name": "Bolivia, Plurinational State of"},{"Code": "BQ", "Name": "Bonaire, Sint Eustatius and Saba"},{"Code": "BA", "Name": "Bosnia and Herzegovina"},{"Code": "BW", "Name": "Botswana"},{"Code": "BV", "Name": "Bouvet Island"},{"Code": "BR", "Name": "Brazil"},{"Code": "IO", "Name": "British Indian Ocean Territory"},{"Code": "BN", "Name": "Brunei Darussalam"},{"Code": "BG", "Name": "Bulgaria"},{"Code": "BF", "Name": "Burkina Faso"},{"Code": "BI", "Name": "Burundi"},{"Code": "KH", "Name": "Cambodia"},{"Code": "CM", "Name": "Cameroon"},{"Code": "CA", "Name": "Canada"},{"Code": "CV", "Name": "Cape Verde"},{"Code": "KY", "Name": "Cayman Islands"},{"Code": "CF", "Name": "Central African Republic"},{"Code": "TD", "Name": "Chad"},{"Code": "CL", "Name": "Chile"},{"Code": "CN", "Name": "China"},{"Code": "CX", "Name": "Christmas Island"},{"Code": "CC", "Name": "Cocos (Keeling) Islands"},{"Code": "CO", "Name": "Colombia"},{"Code": "KM", "Name": "Comoros"},{"Code": "CG", "Name": "Congo"},{"Code": "CD", "Name": "Congo, the Democratic Republic of the"},{"Code": "CK", "Name": "Cook Islands"},{"Code": "CR", "Name": "Costa Rica"},{"Code": "CI", "Name": "C\u00f4te d'Ivoire"},{"Code": "HR", "Name": "Croatia"},{"Code": "CU", "Name": "Cuba"},{"Code": "CW", "Name": "Cura\u00e7ao"},{"Code": "CY", "Name": "Cyprus"},{"Code": "CZ", "Name": "Czech Republic"},{"Code": "DK", "Name": "Denmark"},{"Code": "DJ", "Name": "Djibouti"},{"Code": "DM", "Name": "Dominica"},{"Code": "DO", "Name": "Dominican Republic"},{"Code": "EC", "Name": "Ecuador"},{"Code": "EG", "Name": "Egypt"},{"Code": "SV", "Name": "El Salvador"},{"Code": "GQ", "Name": "Equatorial Guinea"},{"Code": "ER", "Name": "Eritrea"},{"Code": "EE", "Name": "Estonia"},{"Code": "ET", "Name": "Ethiopia"},{"Code": "FK", "Name": "Falkland Islands (Malvinas)"},{"Code": "FO", "Name": "Faroe Islands"},{"Code": "FJ", "Name": "Fiji"},{"Code": "FI", "Name": "Finland"},{"Code": "FR", "Name": "France"},{"Code": "GF", "Name": "French Guiana"},{"Code": "PF", "Name": "French Polynesia"},{"Code": "TF", "Name": "French Southern Territories"},{"Code": "GA", "Name": "Gabon"},{"Code": "GM", "Name": "Gambia"},{"Code": "GE", "Name": "Georgia"},{"Code": "DE", "Name": "Germany"},{"Code": "GH", "Name": "Ghana"},{"Code": "GI", "Name": "Gibraltar"},{"Code": "GR", "Name": "Greece"},{"Code": "GL", "Name": "Greenland"},{"Code": "GD", "Name": "Grenada"},{"Code": "GP", "Name": "Guadeloupe"},{"Code": "GU", "Name": "Guam"},{"Code": "GT", "Name": "Guatemala"},{"Code": "GG", "Name": "Guernsey"},{"Code": "GN", "Name": "Guinea"},{"Code": "GW", "Name": "Guinea-Bissau"},{"Code": "GY", "Name": "Guyana"},{"Code": "HT", "Name": "Haiti"},{"Code": "HM", "Name": "Heard Island and McDonald Islands"},{"Code": "VA", "Name": "Holy See (Vatican City State)"},{"Code": "HN", "Name": "Honduras"},{"Code": "HK", "Name": "Hong Kong"},{"Code": "HU", "Name": "Hungary"},{"Code": "IS", "Name": "Iceland"},{"Code": "IN", "Name": "India"},{"Code": "ID", "Name": "Indonesia"},{"Code": "IR", "Name": "Iran, Islamic Republic of"},{"Code": "IQ", "Name": "Iraq"},{"Code": "IE", "Name": "Ireland"},{"Code": "IM", "Name": "Isle of Man"},{"Code": "IL", "Name": "Israel"},{"Code": "IT", "Name": "Italy"},{"Code": "JM", "Name": "Jamaica"},{"Code": "JP", "Name": "Japan"},{"Code": "JE", "Name": "Jersey"},{"Code": "JO", "Name": "Jordan"},{"Code": "KZ", "Name": "Kazakhstan"},{"Code": "KE", "Name": "Kenya"},{"Code": "KI", "Name": "Kiribati"},{"Code": "KP", "Name": "Korea, Democratic People's Republic of"},{"Code": "KR", "Name": "Korea, Republic of"},{"Code": "KW", "Name": "Kuwait"},{"Code": "KG", "Name": "Kyrgyzstan"},{"Code": "LA", "Name": "Lao People's Democratic Republic"},{"Code": "LV", "Name": "Latvia"},{"Code": "LB", "Name": "Lebanon"},{"Code": "LS", "Name": "Lesotho"},{"Code": "LR", "Name": "Liberia"},{"Code": "LY", "Name": "Libya"},{"Code": "LI", "Name": "Liechtenstein"},{"Code": "LT", "Name": "Lithuania"},{"Code": "LU", "Name": "Luxembourg"},{"Code": "MO", "Name": "Macao"},{"Code": "MK", "Name": "Macedonia, the Former Yugoslav Republic of"},{"Code": "MG", "Name": "Madagascar"},{"Code": "MW", "Name": "Malawi"},{"Code": "MY", "Name": "Malaysia"},{"Code": "MV", "Name": "Maldives"},{"Code": "ML", "Name": "Mali"},{"Code": "MT", "Name": "Malta"},{"Code": "MH", "Name": "Marshall Islands"},{"Code": "MQ", "Name": "Martinique"},{"Code": "MR", "Name": "Mauritania"},{"Code": "MU", "Name": "Mauritius"},{"Code": "YT", "Name": "Mayotte"},{"Code": "MX", "Name": "Mexico"},{"Code": "FM", "Name": "Micronesia, Federated States of"},{"Code": "MD", "Name": "Moldova, Republic of"},{"Code": "MC", "Name": "Monaco"},{"Code": "MN", "Name": "Mongolia"},{"Code": "ME", "Name": "Montenegro"},{"Code": "MS", "Name": "Montserrat"},{"Code": "MA", "Name": "Morocco"},{"Code": "MZ", "Name": "Mozambique"},{"Code": "MM", "Name": "Myanmar"},{"Code": "NA", "Name": "Namibia"},{"Code": "NR", "Name": "Nauru"},{"Code": "NP", "Name": "Nepal"},{"Code": "NL", "Name": "Netherlands"},{"Code": "NC", "Name": "New Caledonia"},{"Code": "NZ", "Name": "New Zealand"},{"Code": "NI", "Name": "Nicaragua"},{"Code": "NE", "Name": "Niger"},{"Code": "NG", "Name": "Nigeria"},{"Code": "NU", "Name": "Niue"},{"Code": "NF", "Name": "Norfolk Island"},{"Code": "MP", "Name": "Northern Mariana Islands"},{"Code": "NO", "Name": "Norway"},{"Code": "OM", "Name": "Oman"},{"Code": "PK", "Name": "Pakistan"},{"Code": "PW", "Name": "Palau"},{"Code": "PS", "Name": "Palestine, State of"},{"Code": "PA", "Name": "Panama"},{"Code": "PG", "Name": "Papua New Guinea"},{"Code": "PY", "Name": "Paraguay"},{"Code": "PE", "Name": "Peru"},{"Code": "PH", "Name": "Philippines"},{"Code": "PN", "Name": "Pitcairn"},{"Code": "PL", "Name": "Poland"},{"Code": "PT", "Name": "Portugal"},{"Code": "PR", "Name": "Puerto Rico"},{"Code": "QA", "Name": "Qatar"},{"Code": "RE", "Name": "R\u00e9union"},{"Code": "RO", "Name": "Romania"},{"Code": "RU", "Name": "Russian Federation"},{"Code": "RW", "Name": "Rwanda"},{"Code": "BL", "Name": "Saint Barth\u00e9lemy"},{"Code": "SH", "Name": "Saint Helena, Ascension and Tristan da Cunha"},{"Code": "KN", "Name": "Saint Kitts and Nevis"},{"Code": "LC", "Name": "Saint Lucia"},{"Code": "MF", "Name": "Saint Martin (French part)"},{"Code": "PM", "Name": "Saint Pierre and Miquelon"},{"Code": "VC", "Name": "Saint Vincent and the Grenadines"},{"Code": "WS", "Name": "Samoa"},{"Code": "SM", "Name": "San Marino"},{"Code": "ST", "Name": "Sao Tome and Principe"},{"Code": "SA", "Name": "Saudi Arabia"},{"Code": "SN", "Name": "Senegal"},{"Code": "RS", "Name": "Serbia"},{"Code": "SC", "Name": "Seychelles"},{"Code": "SL", "Name": "Sierra Leone"},{"Code": "SG", "Name": "Singapore"},{"Code": "SX", "Name": "Sint Maarten (Dutch part)"},{"Code": "SK", "Name": "Slovakia"},{"Code": "SI", "Name": "Slovenia"},{"Code": "SB", "Name": "Solomon Islands"},{"Code": "SO", "Name": "Somalia"},{"Code": "ZA", "Name": "South Africa"},{"Code": "GS", "Name": "South Georgia and the South Sandwich Islands"},{"Code": "SS", "Name": "South Sudan"},{"Code": "ES", "Name": "Spain"},{"Code": "LK", "Name": "Sri Lanka"},{"Code": "SD", "Name": "Sudan"},{"Code": "SR", "Name": "Suriname"},{"Code": "SJ", "Name": "Svalbard and Jan Mayen"},{"Code": "SZ", "Name": "Swaziland"},{"Code": "SE", "Name": "Sweden"},{"Code": "CH", "Name": "Switzerland"},{"Code": "SY", "Name": "Syrian Arab Republic"},{"Code": "TW", "Name": "Taiwan, Province of China"},{"Code": "TJ", "Name": "Tajikistan"},{"Code": "TZ", "Name": "Tanzania, United Republic of"},{"Code": "TH", "Name": "Thailand"},{"Code": "TL", "Name": "Timor-Leste"},{"Code": "TG", "Name": "Togo"},{"Code": "TK", "Name": "Tokelau"},{"Code": "TO", "Name": "Tonga"},{"Code": "TT", "Name": "Trinidad and Tobago"},{"Code": "TN", "Name": "Tunisia"},{"Code": "TR", "Name": "Turkey"},{"Code": "TM", "Name": "Turkmenistan"},{"Code": "TC", "Name": "Turks and Caicos Islands"},{"Code": "TV", "Name": "Tuvalu"},{"Code": "UG", "Name": "Uganda"},{"Code": "UA", "Name": "Ukraine"},{"Code": "AE", "Name": "United Arab Emirates"},{"Code": "GB", "Name": "United Kingdom"},{"Code": "US", "Name": "United States"},{"Code": "UM", "Name": "United States Minor Outlying Islands"},{"Code": "UY", "Name": "Uruguay"},{"Code": "UZ", "Name": "Uzbekistan"},{"Code": "VU", "Name": "Vanuatu"},{"Code": "VE", "Name": "Venezuela, Bolivarian Republic of"},{"Code": "VN", "Name": "Viet Nam"},{"Code": "VG", "Name": "Virgin Islands, British"},{"Code": "VI", "Name": "Virgin Islands, U.S."},{"Code": "WF", "Name": "Wallis and Futuna"},{"Code": "EH", "Name": "Western Sahara"},{"Code": "YE", "Name": "Yemen"},{"Code": "ZM", "Name": "Zambia"},{"Code": "ZW", "Name": "Zimbabwe"}];

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

            d.supportedInAppLanguages.forEach(( lan: string ) => {
              if(!languages.find(x => x === lan.split('_')[0])){
                languages.push(lan.split('_')[0].toUpperCase());
              }
            })

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

          setApps(tempApps);
          console.log(languages);
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

            <div>
              <h3>Headsets</h3>
              <p>
                Click to toggle selection.<br />
                Shift + Click to select only the one you clicked on.
              </p>
            </div>
          </div>

          <div class="headset-selection-list" ref={( el ) => filterTypesEl = el}></div>

          <hr />
          <div>
            <h3>App Languages</h3>
          </div>

          <div ref={( el ) => languageFilterEl = el}>Loading...</div>
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