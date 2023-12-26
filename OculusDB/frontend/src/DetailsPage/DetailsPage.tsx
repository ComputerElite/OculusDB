import { Match, Show, Switch, For, createSignal, onMount } from 'solid-js';
import './DetailsPage.css'

class DetailsPageProps{
  currentID!: () => string;
}

const comfortRatingIcons = [
  'https://cdn.phazed.xyz/odbicons/comfortable.svg',
  'https://cdn.phazed.xyz/odbicons/moderate.svg',
  'https://cdn.phazed.xyz/odbicons/intense.svg',
  'https://cdn.phazed.xyz/odbicons/norate.svg',
]

let headsets: Array<any> = [];
let headsetTypes: Array<any> = [];

let DetailsPage = ( props: DetailsPageProps ) => {
  let container: HTMLElement;

  onMount(async () => {
    let req = await fetch('https://oculusdb-rewrite.rui2015.me/api/v2/id/'+props.currentID());
    let res = await req.json();

    container.innerHTML = '';
    let supportedHeadsetsEl: HTMLElement;

    let displayApplication = async ( app: any ) => {
      let [ appTab, setAppTab ] = createSignal(0);

      let tabButtons: Array<HTMLElement> = [];
      let selectTab = ( index: number ) => {
        tabButtons.forEach(btn => btn.classList.remove('button-selected'));
        tabButtons[index].classList.add('button-selected');

        setAppTab(index);
      }

      let connectedReq = await fetch('https://oculusdb-rewrite.rui2015.me/api/v2/connected/'+app.id);
      let connected = await connectedReq.json();
      console.log(connected);

      let selectedOffer = app.offers[0];
      document.querySelector('title')!.innerText = app.displayName + ' - Oculus DB';
      container.appendChild(<div>
        <div class="app-keywords">{ app.keywords.map(( x: string ) => x[0].toUpperCase() + x.substring(1, x.length)).join(', ') }</div>
        <div class="app-rating">
          <img width="20" src={ comfortRatingIcons[app.comfortRating] } />
          <div style={{ "margin-left": '10px' }}>{ app.comfortRatingFormatted }</div>
        </div>

        <div class="header-image" style={{ background: 'url(' + app.imgUrlAbsolute + ')' }}></div>

        <div class="app-branding">
          <div class="app-icon" style={{ background: 'url(' + app.imgUrlAbsolute + ')' }}></div>
          <div class="app-title">
            <h1>{ app.displayName }</h1>
            <p>
              { app.longDescription }
            </p>
          </div>
        </div>

        <div class="app-supported-devices" ref={( el ) => supportedHeadsetsEl = el}>Supported Devices: {
          /* This will probably break in the future, waiting for a diffent list endpoint to be available */ headsetTypes[app.group] ? headsetTypes[app.group].name : 'Loading...'
        }</div>

        <div class="app-column">
          <div class="app-lists">
            <div class="app-list-select">
              <div class="list-select button button-selected" ref={( el ) => tabButtons.push(el)} onClick={() => selectTab(0)}>Versions ({ connected.versions.length })</div>
              <div class="list-select button" ref={( el ) => tabButtons.push(el)} onClick={() => selectTab(1)}>DLCs ({ connected.iapItems.length })</div>
              <div class="list-select button" ref={( el ) => tabButtons.push(el)} onClick={() => selectTab(2)}>DLC Packs ({ connected.iapItemPacks.length })</div>
              <div class="list-select button" ref={( el ) => tabButtons.push(el)} onClick={() => selectTab(3)}>Applications ({ connected.applications.length })</div>
            </div><br />

            <Switch>
              <Match when={appTab() === 0}>
                <For each={connected.versions}>
                  {( v, index ) => <div>
                    <style>
                      {`#endpoint-dropdown-${index()}:checked ~ .dropdown{
                        height: fit-content;
                      }

                      #endpoint-dropdown-${index()}:checked ~ .dropdown > .dropdown-contents-version${index()}{
                        display: block;
                        height: fit-content;
                        opacity: 1;
                      }

                      #endpoint-dropdown-${index()}:checked ~ .dropdown .dropdown-heading-version${index()} i{
                        rotate: 90deg;
                      }`}
                    </style>

                    <input type="checkbox" id={"endpoint-dropdown-"+index()} style={{ display: 'none' }} />
                    <div class="dropdown">
                      <label for={"endpoint-dropdown-"+index()}>
                        <div class={"dropdown-heading dropdown-heading-version"+index()}>
                          <p>
                            <i class="fa-solid fa-circle-arrow-right"></i> { v.version }
                          </p>
                        </div>
                      </label>

                      <div class={"dropdown-contents dropdown-contents-version"+index()}>
                        <Show when={ v.downloadable === false }><b>Developer Only Build.</b><br /></Show>
                        <Show when={ v.changelog !== null }><span class="version-key">Changelog:</span> { v.changelog === "" ? "None" : v.changelog }<br /></Show>

                        <span class="version-key">Size:</span> { v.sizeFormatted }<br />
                        <span class="version-key">Required Space:</span> { v.requiredSpaceFormatted }<br />
                        <span class="version-key">Uploaded:</span> { new Date(v.uploadedDate).toString() }<br />
                        <Show when={ v.downloadable !== false }><span class="version-key">Release Channel:</span> { v.releaseChannels.map((x: any) => x.name).join(', ') }<br /></Show>
                        <span class="version-key">ID:</span> { v.id }<br />
                        <span class="version-key">Version Code:</span> { v.versionCode }<br />

                        <Show when={v.obbBinary !== null}>
                          <br />
                          <h2 style={{ margin: '0' }}>OBBs</h2>

                          <span class="version-key">ID:</span> { v.obbBinary.id }<br />
                          <span class="version-key">Size:</span> { v.obbBinary.sizeFormatted }<br />
                          <span class="version-key">Segmented:</span> { v.obbBinary.isSegmented ? <i class="fa-solid fa-check"></i> : <i class="fa-solid fa-xmark"></i> }<br />
                          <span class="version-key">File Name:</span> { v.obbBinary.filename }<br /><br />

                          <span class="version-key">Last Updated:</span> { v.obbBinary.__lastUpdated }<br />
                          <span class="version-key">Scraped By:</span> { v.obbBinary.__sn }
                        </Show>

                        <br />
                        <span class="version-key">Last Scraped:</span> { new Date(v.__lastUpdated).toString() }<br />
                        <span class="version-key">Last Priority Scraped:</span> { new Date(v.__lastPriorityScrape).toString() }<br />
                        <span class="version-key">Scraped By:</span> { v.__sn }<br />
                      </div>
                    </div>
                    <br />
                  </div>}
                </For>
              </Match>
              <Match when={appTab() === 1}>
                <p>1</p>
              </Match>
              <Match when={appTab() === 2}>
                <p>2</p>
              </Match>
              <Match when={appTab() === 3}>
                <p>3</p>
              </Match>
            </Switch>
          </div>
          <div class="app-info">
            <h2><b>{ app.displayName }</b></h2>
            <hr />

            <h2>Price</h2>
            <div>
              <Show when={selectedOffer.strikethroughPrice !== null}>
                <span style={{ "text-decoration": 'line-through', color: 'rgb(170, 170, 170)' }}>{ selectedOffer.strikethroughPrice.price === 0 ? 'Free' : selectedOffer.strikethroughPrice.priceFormatted }</span>
              </Show>&nbsp;&nbsp;
              { selectedOffer.price.price === 0 ? 'Free' : selectedOffer.price.priceFormatted }
            </div><br />

            <div class="price-info" style={{ "text-align": 'left' }}>
              <b>Last Updated:</b> { new Date(selectedOffer.__lastUpdated).toString() }<br /><br />
              <b>Scraped By:</b> { selectedOffer.__sn ? selectedOffer.__sn : "None" }<br /><br />
              <b>Offer ID:</b> { selectedOffer.id }
            </div>
            <hr />

            <h2>Info</h2>
            <div style={{ "text-align": 'left' }}>
              <b>Publisher:</b> { app.publisherName }<br /><br />
              <b>Package Name:</b> { app.packageName }<br /><br />
              <b>Canonical Name:</b> { app.canonicalName }<br /><br />
              <b>Last Updated:</b> { new Date(app.__lastUpdated).toString() }<br /><br />
              <b>Scraped By:</b> { app.__sn }<br /><br />
              <b>Website:</b> <a target="_blank" href={ app.websiteUrl }>{ app.websiteUrl }</a>
            </div>
          </div>
        </div>
      </div> as Node);
    }

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
    
        if(supportedHeadsetsEl)
          supportedHeadsetsEl.innerHTML = 'Supported Headsets: ' + headsetTypes[res.group].name
      })

    switch(res.__OculusDBType){
      case 'Application':
        displayApplication(res)
        break;
      case 'Offer':
        let appReq = await fetch('https://oculusdb-rewrite.rui2015.me/api/v2/id/'+res.parentApplication.id);
        let appRes = await appReq.json();

        appRes.offers = [ res ];
        displayApplication(appRes);

        break;
      default:
        console.log(res);
        document.querySelector('title')!.innerText = '404 Not Found - Oculus DB';

        container.appendChild(<div>
          <div class="big-text">404</div>
          <div class="slightly-smaller-text">Page Not Found</div>

          <p style={{ 'text-align': 'center' }}>
            I couldn't find that page... Double check the URL is correct.
          </p>
        </div> as Node);
        break;
    }
  });

  return (
    <div class="main">
      <div class="info" ref={( el ) => container = el}>
        <h1 style={{ "text-align": 'center' }}>Loading...</h1>
      </div>
    </div>
  )
}

export default DetailsPage