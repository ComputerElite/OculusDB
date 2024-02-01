import { Match, Show, Switch, For, createSignal, onMount } from 'solid-js';
import './DetailsPage.css'

class DetailsPageProps{
  currentID!: () => string;
}

const comfortRatingIcons = [
  '/assets/comfortable.svg',
  '/assets/moderate.svg',
  '/assets/intense.svg',
  '/assets/norate.svg',
]

const headsetGroups = [ // This will be updated at some point to use an api endpoint
  'Quest',
  'PCVR',
  'Go & GearVR'
]

let headsets: Array<any> = [];
let headsetTypes: Array<any> = [];

let formatString = ( str: string ) => {
  if(str == null) return "- not fetched -"
  if(str == "") return "-"
  return str
}

let formatBool = ( bool: boolean ) => {
  if(bool == null) return "- not fetched -"
  return bool ? "True" : "False"
}

let formatStringArray = ( arr: Array<string> ) => {
  if(arr == null) return "- not fetched -"
  if(arr.length == 0) return "-"
  return arr.join(', ')
}

let DetailsPage = ( props: DetailsPageProps ) => {
  let container: HTMLElement;

  onMount(async () => {
    let req = await fetch('https://oculusdb-rewrite.rui2015.me/api/v2/id/'+props.currentID());
    let res = await req.json();

    container.innerHTML = '';
    let supportedHeadsetsEl: HTMLElement;

    let displayApplication = async ( app: any ) => {
      let [ appTab, setAppTab ] = createSignal(0);
      let [ starred, setStarred ] = createSignal(false);
      let [ showDevOnly, setShowDevOnly ] = createSignal(false);

      let starredApps = localStorage.getItem('starred') ? localStorage.getItem('starred')!.split(',') : [];
      setStarred(starredApps.find(x => x === app.id) ? true : false);
    
      let tabButtons: Array<HTMLElement> = [];
      let selectTab = ( index: number ) => {
        tabButtons.forEach(btn => btn.classList.remove('button-selected'));
        tabButtons[index].classList.add('button-selected');

        localStorage.setItem('application-selected-tab', index.toString());
        setAppTab(index);
      }

      let star = () => {
        setStarred(!starred());

        starredApps = localStorage.getItem('starred') ? localStorage.getItem('starred')!.split(',') : [];
        
        if(starredApps.find(x => x === app.id))
          starredApps = starredApps.filter(x => x !== app.id);
        else
          starredApps.push(app.id);

        localStorage.setItem('starred', starredApps.join(','));
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
        <div class="app-save" onClick={star}>
          <Switch>
            <Match when={starred()}>
              <i class="fa-solid fa-star"></i>
            </Match>
            <Match when={!starred()}>
              <i class="fa-regular fa-star"></i>
            </Match>
          </Switch>
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
          /* This will probably break in the future, waiting for a different list endpoint to be available */ headsetTypes[app.group] ? headsetTypes[app.group].name : 'Loading...'
        }</div>

        <div class="app-column">
          <div class="app-lists">
            <div class="app-list-select">
              <div class="list-select button" ref={( el ) => tabButtons.push(el)} onClick={() => selectTab(0)}>Versions ({ connected.versions.length })</div>
              <div class="list-select button" ref={( el ) => tabButtons.push(el)} onClick={() => selectTab(1)}>DLCs ({ connected.iapItems.length })</div>
              <div class="list-select button" ref={( el ) => tabButtons.push(el)} onClick={() => selectTab(2)}>DLC Packs ({ connected.iapItemPacks.length })</div>
              <div class="list-select button" ref={( el ) => tabButtons.push(el)} onClick={() => selectTab(3)}>Applications ({ connected.applications.length })</div>
            </div><br />

            <Switch>
              <Match when={appTab() === 0}>
                <div class="list-select button" style={{ width: 'calc(100% - 60px)' }} onClick={() => {setShowDevOnly(!showDevOnly()); console.log(showDevOnly());}}>
                  Show all versions
                </div><br /><br />

                <For each={connected.versions}>
                  {(v, index) => <div>
                    <style>
                      {`#version-dropdown-${index()}:checked ~ .dropdown{
                        height: fit-content;
                      }

                      #version-dropdown-${index()}:checked ~ .dropdown > .dropdown-contents-version${index()}{
                        display: block;
                        height: fit-content;
                        opacity: 1;
                      }

                      #version-dropdown-${index()}:checked ~ .dropdown .dropdown-heading-version${index()} i{
                        rotate: 90deg;
                      }`}
                    </style>
                    <Show when={showDevOnly() || v.downloadable}>
                      <input type="checkbox" id={"version-dropdown-" + index()} style={{display: 'none'}}/>
                      <div class="dropdown">
                        <label for={"version-dropdown-" + index()}>
                          <div class={"dropdown-heading dropdown-heading-version" + index()}>
                            <p>
                              <i class="fa-solid fa-circle-arrow-right"></i> <span
                                class={v.downloadable ? '' : 'dev-only'}>{v.version + (v.alias != null ? (
                                <b>{v.alias}</b>) : '')}</span>
                            </p>
                          </div>
                        </label>

                        <div class={"dropdown-contents dropdown-contents-version" + index()}>
                          <Show when={v.downloadable === false}><b>Developer Only Build.</b><br/></Show>
                          <Show when={v.changelog !== null}><span
                              class="version-key">Changelog:</span> {v.changelog === "" ? "None" : v.changelog}<br/></Show>

                          <span class="version-key">Size:</span> {v.sizeFormatted}<br/>
                          <span class="version-key">Required Space:</span> {v.requiredSpaceFormatted}<br/>
                          <span class="version-key">Uploaded:</span> {new Date(v.uploadedDate).toString()}<br/>
                          <Show when={v.downloadable !== false}><span
                              class="version-key">Release Channel:</span> {v.releaseChannels.map((x: any) => x.name).join(', ')}<br/></Show>
                          <span class="version-key">ID:</span> {v.id}<br/>
                          <span class="version-key">Version Code:</span> {v.versionCode}<br/>

                          <Show when={v.obbBinary !== null}>
                            <br/>
                            <h2 style={{margin: '0'}}>OBBs</h2>

                            <span class="version-key">ID:</span> {v.obbBinary.id}<br/>
                            <span class="version-key">Size:</span> {v.obbBinary.sizeFormatted}<br/>
                            <span class="version-key">Segmented:</span> {v.obbBinary.isSegmented ?
                              <i class="fa-solid fa-check"></i> : <i class="fa-solid fa-xmark"></i>}<br/>
                            <span class="version-key">File Name:</span> {v.obbBinary.filename}<br/><br/>

                            <span class="version-key">Last Updated:</span> {v.obbBinary.__lastUpdated}<br/>
                            <span class="version-key">Scraped By:</span> {v.obbBinary.__sn}
                          </Show>

                          <br/>
                          <span class="version-key">Last Scraped:</span> {new Date(v.__lastUpdated).toString()}<br/>
                          <span
                              class="version-key">Last Priority Scraped:</span> {new Date(v.__lastPriorityScrape).toString()}<br/>
                          <span class="version-key">Scraped By:</span> {v.__sn}<br/>
                        </div>
                      </div>
                      <br/>
                    </Show>
                  </div>}
                </For>
              </Match>
              <Match when={appTab() === 1}>
                <For each={connected.iapItems}>
                  {(dlc, index) => <div>
                    <style>
                      {`#iapItems-dropdown-${index()}:checked ~ .dropdown{
                        height: fit-content;
                      }

                      #iapItems-dropdown-${index()}:checked ~ .dropdown > .dropdown-contents-iapItems${index()}{
                        display: block;
                        height: fit-content;
                        opacity: 1;
                      }

                      #iapItems-dropdown-${index()}:checked ~ .dropdown .dropdown-heading-iapItems${index()} i{
                        rotate: 90deg;
                      }`}
                    </style>
                    <input type="checkbox" id={"iapItems-dropdown-" + index()} style={{display: 'none'}}/>
                      <div class="dropdown">
                        <label for={"iapItems-dropdown-" + index()}>
                          <div class={"dropdown-heading dropdown-heading-iapItems" + index()}>
                            <p>
                              <i class="fa-solid fa-circle-arrow-right"></i> <span>{ dlc.displayName }</span>
                            </p>
                          </div>
                        </label>

                        <div class={"dropdown-contents dropdown-contents-iapItems" + index()}>

                        </div>
                      </div><br />
                  </div>}
                </For>
              </Match>
              <Match when={appTab() === 2}>
                <For each={connected.iapItemPacks}>
                  {(dlcPack, index) => <div>
                    <style>
                      {`#iapItemPacks-dropdown-${index()}:checked ~ .dropdown{
                        height: fit-content;
                      }

                      #iapItemPacks-dropdown-${index()}:checked ~ .dropdown > .dropdown-contents-iapItemPacks${index()}{
                        display: block;
                        height: fit-content;
                        opacity: 1;
                      }

                      #iapItemPacks-dropdown-${index()}:checked ~ .dropdown .dropdown-heading-iapItemPacks${index()} i{
                        rotate: 90deg;
                      }`}
                    </style>
                    <input type="checkbox" id={"iapItemPacks-dropdown-" + index()} style={{display: 'none'}}/>
                      <div class="dropdown">
                        <label for={"iapItemPacks-dropdown-" + index()}>
                          <div class={"dropdown-heading dropdown-heading-iapItemPacks" + index()}>
                            <p>
                              <i class="fa-solid fa-circle-arrow-right"></i> <span>{ dlcPack.displayName }</span>
                            </p>
                          </div>
                        </label>

                        <div class={"dropdown-contents dropdown-contents-iapItemPacks" + index()}>
                          { dlcPack.displayShortDescription }<br /><br />

                          <span class="version-key">Price: </span> {dlcPack.offers[0].price.priceFormatted}<br />
                          <span class="version-key">Included DLCs: </span> {dlcPack.items.length}
                        </div>
                      </div><br />
                  </div>}
                </For>
              </Match>
              <Match when={appTab() === 3}>
                <For each={connected.applications}>
                  {(app, index) => <div>
                    <style>
                      {`#application-dropdown-${index()}:checked ~ .dropdown{
                        height: fit-content;
                      }

                      #application-dropdown-${index()}:checked ~ .dropdown > .dropdown-contents-application${index()}{
                        display: block;
                        height: fit-content;
                        opacity: 1;
                      }

                      #application-dropdown-${index()}:checked ~ .dropdown .dropdown-heading-application${index()} i{
                        rotate: 90deg;
                      }`}
                    </style>
                    <input type="checkbox" id={"application-dropdown-" + index()} style={{display: 'none'}}/>
                      <div class="dropdown">
                        <label for={"application-dropdown-" + index()}>
                          <div class={"dropdown-heading dropdown-heading-application" + index()}>
                            <p>
                              <i class="fa-solid fa-circle-arrow-right"></i> <span>{ app.displayName }</span>
                            </p>
                          </div>
                        </label>

                        <div class={"dropdown-contents dropdown-contents-application" + index()}>
                          <span class="version-key">Devices:</span> {headsetGroups[app.group]}<br />

                          <br />
                          <div class="button" style={{ width: 'calc(100% - 55px)', 'text-align': 'center' }} onClick={ () => window.location.href = '/id/' + app.id }>
                            Go to Details
                          </div>
                        </div>
                      </div><br />
                  </div>}
                </For>
              </Match>
            </Switch>
          </div>
          <div class="app-info">
            <h2><b>{app.displayName}</b></h2>
            <hr/>

            <h2>Price</h2>
            <div>
              <Show when={selectedOffer.strikethroughPrice !== null}>
                <span style={{
                  "text-decoration": 'line-through',
                  color: 'rgb(170, 170, 170)'
                }}>{selectedOffer.strikethroughPrice.price === 0 ? 'Free' : selectedOffer.strikethroughPrice.priceFormatted}</span>
              </Show>&nbsp;&nbsp;
              {selectedOffer.price.price === 0 ? 'Free' : selectedOffer.price.priceFormatted}
            </div>
            <br/>

            <div class="price-info" style={{"text-align": 'left'}}>
              <b>Last Updated:</b> {new Date(selectedOffer.__lastUpdated).toString()}<br/><br/>
              <b>Scraped By:</b> {selectedOffer.__sn ? selectedOffer.__sn : "None"}<br/><br/>
              <b>Offer ID:</b> {selectedOffer.id}
            </div>
            <hr/>

            <h2>Info</h2>
            <div style={{"text-align": 'left'}}>
              <b>Publisher:</b> {formatString(app.publisherName)}<br/><br/>
              <b>Developer:</b> {formatString(app.developerName)}<br/><br/>
              <b>Package Name:</b> {formatString(app.packageName)}<br/><br/>
              <b>Canonical Name:</b> {formatString(app.canonicalName)}<br/><br/>
              <b>External subscription:</b> {formatString(app.externalSubscriptionTypeFormatted)}<br/><br/>
              <b>Play area:</b> {formatString(app.playAreaFormatted)}<br/><br/>
              <b>Canonical name:</b> {formatString(app.canonicalName)}<br/><br/>
              <b>Has in-app ads:</b> {formatBool(app.hasInAppAds)}<br/><br/>
              <b>Is AppLab:</b> {formatBool(app.isAppLab)}<br/><br/>
              <b>Is Quest for business:</b> {formatBool(app.isQuestForBusiness)}<br/><br/>
              <b>Is test:</b> {formatBool(app.isTest)}<br/><br/>
              <b>Is blocked by verification:</b> {formatBool(app.isBlockedByVerification)}<br/><br/>
              <b>Is for Oculus keys only:</b> {formatBool(app.isForOculusKeysOnly)}<br/><br/>
              <b>Is first party:</b> {formatBool(app.isFirstParty)}<br/><br/>
              <b>Cloud backup enabled:</b> {formatBool(app.cloudBackupEnabled)}<br/><br/>

              <b>Supported input devices:</b> {formatStringArray(app.supportedInputDevicesFormatted)}<br/><br/>
              <b>Supported player modes:</b> {formatStringArray(app.supportedPlayerModesFormatted)}<br/><br/>
              <b>User interaction modes:</b> {formatStringArray(app.userInteractionModesFormatted)}<br/><br/>
              <b>Share capabilities:</b> {formatStringArray(app.shareCapabilitiesFormatted)}<br/><br/>
              <b>Supported languages:</b> {formatStringArray(app.supportedInAppLanguages)}<br/><br/>
              <b>Website:</b> <a target="_blank" href={app.websiteUrl}>{formatString(app.websiteUrl)}</a><br/><br/>
              <b>Support website:</b> <a target="_blank"
                                        href={app.supportWebsiteUrl}>{formatString(app.supportWebsiteUrl)}</a><br/><br/>
              <b>Terms of service:</b> <a target="_blank"
                                         href={app.developerTermsOfServiceUrl}>{formatString(app.developerTermsOfServiceUrl)}</a><br/><br/>
              <b>Privacy policy:</b> <a target="_blank"
                                       href={app.developerPrivacyPolicyUrl}>{formatString(app.developerPrivacyPolicyUrl)}</a><br/><br/>
              <b>Last Updated:</b> {new Date(app.__lastUpdated).toString()}<br/><br/>
              <b>Scraped By:</b> {app.__sn}<br/><br/>
            </div>

            <h2>Scraping errors</h2>
            <div style={{"text-align": 'left'}}>
              <For each={app.errors}>
                {(e) => <div>

                    <b>Error type:</b> {formatString(e.typeFormatted)}<br/><br/>
                  <b>Reason:</b> {formatString(e.reasonFormatted)}<br/><br/>
                  <b>Unknown or approximated fields:</b> {formatStringArray(e.unknownOrApproximatedFieldsIfAny)}<br/><br/>
                  <hr/>
                </div>
                }
              </For>
            </div>
          </div>
        </div>
      </div> as Node);

      selectTab(localStorage.getItem('application-selected-tab') !== undefined ? parseInt(localStorage.getItem('application-selected-tab')!) : 0);
    }

    fetch('https://oculusdb-rewrite.rui2015.me/api/v2/lists/headsets')
        .then(data => data.json())
        .then(data => {
          headsets = data;
          headsetTypes = [];

          headsets.forEach(headset => {
            let type = headsetTypes.find(x => x.group === headset.groupString);

            if (type) {
              type.name += ', ' + headset.displayName;
            } else {
              headsetTypes.push({group: headset.groupString, name: headset.displayName });
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