document.head.innerHTML += `<link href="https://fonts.googleapis.com/css?family=Open+Sans:400,400italic,700,700italic" rel="stylesheet" type="text/css">`

document.body.innerHTML = `<div class="navBar">
<div class="navBarInner">
    <img class="navBarElement" src="https://computerelite.github.io/assets/CE_512px.png" style="height: 100%;">
    <div class="navBarElement">
        OculusDB
    </div>
</div>
<div class="navBarInner" style="flex-direction: row-reverse;">
    <div class="navBarElement">
        <input type="text" placeholder="Query" id="query">
        <input type="button" onclick="Search('query')" value="Search">
    </div>
</div>
</div>` + document.body.innerHTML

const SearchResults = document.getElementById("searchResults")
const loader = `<div class="centerIt">
<div class="loader"></div>
</div>`

function openTab(evt, tab) {
    // Declare all variables
    var i, tabcontent, tablinks;
  
    // Get all elements with class="tabcontent" and hide them
    tabcontent = document.getElementsByClassName("tabcontent");
    for (i = 0; i < tabcontent.length; i++) {
      tabcontent[i].style.display = "none";
    }
  
    // Get all elements with class="tablinks" and remove the class "active"
    tablinks = document.getElementsByClassName("tablinks");
    for (i = 0; i < tablinks.length; i++) {
      tablinks[i].className = tablinks[i].className.replace(" active", "");
    }
  
    // Show the current tab, and add an "active" class to the button that opened the tab
    document.getElementById(tab).style.display = "block";
    evt.currentTarget.className += " active";
  }

document.getElementById("query").onkeydown = e => {
    if(e.code == "Enter") {
        Search("query")
    }
}
const params = new URLSearchParams(window.location.search)
if(params.get("query")) InternalSearch(params.get("query"))

function InternalSearch(query) {
    document.getElementById("query").value = query
    SearchResults.innerHTML = loader
    fetch("/api/search/" + query).then(res => {
        res.json().then(res => {
            SearchResults.innerHTML = ""
            res.forEach(x => {
                SearchResults.innerHTML += FormatApplication(x)
            })
            if(SearchResults.innerHTML == "") {
                SearchResults.innerHTML = `
                    <div class="application centerIt" style="cursor: default;">
                        <b>No Results</b>
                    </div>
                `
            }
        })
    })
}

function GetObjectById(id) {
    return new Promise((resolve, reject) => {
        fetch("/api/id/" + id).then(res => {
            if(res.status != 200) reject(res.status)
            res.json().then(res => resolve(res))
        })
    })
    
}

function GetActivityById(id) {
    return new Promise((resolve, reject) => {
        fetch("/api/activityid/" + id).then(res => {
            if(res.status != 200) reject(res.status)
            res.json().then(res => resolve(res))
        })
    })
    
}

function Search(element)
{
    var query = document.getElementById(element).value
    window.location = "/search?query=" + query
}

function OpenApplication(id) {
    window.location = "/id/" + id
}

function OpenActivity(id) {
    window.location = "/activity/" + id
}

function FormatApplication(application, htmlId = "") {
    return `<div class="application">
    <div>
        <div class="flex">
            <div style="padding: 15px; font-weight: bold; color: var(--highlightedColor);" onclick="RevealDescription('${htmlId}_${application.id}')" id="${htmlId}_${application.id}_trigger" class="anim noselect">&gt;</div>
            <div stlye="font-size: 1.25em;">${application.displayName}</div>
        </div>
        <div class="hidden" id="${htmlId}_${application.id}">
            
            <table>
                <tr><td>Description</td><td>${application.display_long_description.replace("\n", "<br>")}</td></tr>
                <tr><td>Current price</td><td>${application.current_offer.price.formatted}</td></tr>
                <tr><td>Baseline price</td><td>${application.baseline_offer.price.offset_amount != 0 && application.baseline_offer.price.offset_amount < application.current_offer.price.offset_amount ? application.baseline_offer.price.formatted : application.current_offer.price.formatted}</td></tr>
                <tr><td>Rating</td><td>${application.quality_rating_aggregate.toFixed(2)}</td></tr>
                <tr><td>Supported Headsets</td><td>${GetHeadsets(application.supported_hmd_platforms)}</td></tr>
                <tr><td>Publisher</td><td>${application.publisher_name}</td></tr>
                <tr><td>Canonical name</td><td>${application.canonicalName}</td></tr>
                <tr><td>Id</td><td>${application.id}</td></tr>
            </table>
        </div>
    </div>
    <div>
        <input type="button" value="Details" onclick="OpenApplication('${application.id}')">
    </div>
</div>`
}

function FormatDLC(dlc, htmlid = "") {
    if(htmlid == "") htmlid = dlc.id
    return `<div class="application">
    <div>
        <div class="flex">
            <div style="padding: 15px; font-weight: bold; color: var(--highlightedColor);" onclick="RevealDescription('${htmlid}')" id="${htmlid}_trigger" class="anim noselect">&gt;</div>
            <div stlye="font-size: 1.25em;">${dlc.display_name}</div>
        </div>
        <div class="hidden" id="${htmlid}">
            
            <table>
                <tr><td>Description</td><td>${dlc.display_short_description.replace("\n", "<br>")}</td></tr>
                <tr><td>Price</td><td>${dlc.current_offer.price.formatted}</td></tr>
                <tr><td>Id</td><td>${dlc.id}</td></tr>
            </table>
        </div>
    </div>
    <div>
        <input type="button" value="Download" onclick="window.open('https://securecdn.oculus.com/binaries/download/?id=${dlc.id}', '_blank')">
    </div>
</div>`
}

function FormatDLCPack(dlc, dlcs) {
    var included = ""
    dlc.bundle_items.edges.forEach(d => {
        included += FormatDLC(GetDLC(dlcs, d.node.id), dlc.id + "_" + d.node.id)
    })
    return `<div class="application">
    <div>
        <div class="flex">
            <div style="padding: 15px; font-weight: bold; color: var(--highlightedColor);" onclick="RevealDescription('${dlc.id}')" id="${dlc.id}_trigger" class="anim noselect">&gt;</div>
            <div stlye="font-size: 1.25em;">${dlc.display_name}</div>
        </div>
        <div class="hidden" id="${dlc.id}">
            
            <table>
                <tr><td>Description</td><td>${dlc.display_short_description.replace("\n", "<br>")}</td></tr>
                <tr><td>Price</td><td>${dlc.current_offer.price.formatted}</td></tr>
                <tr><td>Included DLCs</td><td>${included}</td></tr>
                <tr><td>Id</td><td>${dlc.id}</td></tr>
            </table>
        </div>
    </div>
    <div>
        <input type="button" value="Download" onclick="window.open('https://securecdn.oculus.com/binaries/download/?id=${dlc.id}', '_blank')">
    </div>
</div>`
}


function FormatDLCActivity(a) {
    return `<div class="application">
    <div>
        <div class="flex">
            <div style="padding: 15px; font-weight: bold; color: var(--highlightedColor);" onclick="RevealDescription('${a.__id}')" id="${a.__id}_trigger" class="anim noselect">&gt;</div>
            <div stlye="font-size: 1.25em;">DLC <b>${a.displayName}</b> ${a.__OculusDBType == "ActivityNewDLC" ? " has been added to " : " has been updated for "} <b>${a.parentApplication.displayName}</b></div>
        </div>
        <div class="hidden" id="${a.__id}">
            <table>
                <tr><td>Description</td><td>${a.displayShortDescription.replace("\n", "<br>")}</td></tr>
                <tr><td>Price</td><td>${a.priceFormatted}</td></tr>
                <tr><td>DLC id</td><td>${a.id}</td></tr>
                <tr><td>Parent Application</td><td>${FormatParentApplication(a.parentApplication, a.__id)}</td></tr>
                <tr><td>Activity id</td><td>${a.__id}</td></tr>
            </table>
        </div>
    </div>
    <div>
        <input type="button" value="Details" onclick="OpenActivity('${a.__id}')">
    </div>
</div>`
}

function FormatDLCPackActivityDLC(a, i) {
    return `<div class="application">
    <div>
        <div class="flex">
            <div style="padding: 15px; font-weight: bold; color: var(--highlightedColor);" onclick="RevealDescription('${i}_${a.__id}')" id="${i}_${a.__id}_trigger" class="anim noselect">&gt;</div>
            <div stlye="font-size: 1.25em;"></div>
        </div>
        <div class="hidden" id="${i}_${a.__id}">
            <table>
                <tr><td>Description</td><td>${a.displayShortDescription.replace("\n", "<br>")}</td></tr>
                <tr><td>DLC id</td><td>${a.id}</td></tr>
            </table>
        </div>
    </div>
    <div>
        <input type="button" value="Details" onclick="OpenApplication('${a.id}')">
    </div>
</div>`
}

function FormatDLCPackActivity(a) {
    var included = ""
    a.includedDLCs.forEach(d => {
        included += FormatDLCPackActivityDLC(d, a.__id)
    })
    return `<div class="application">
    <div>
        <div class="flex">
            <div style="padding: 15px; font-weight: bold; color: var(--highlightedColor);" onclick="RevealDescription('${a.id}')" id="${a.id}_trigger" class="anim noselect">&gt;</div>
            <div stlye="font-size: 1.25em;">DLC Pack ${a.displayName} ${a.__OculusDBType == "ActivityNewDLCPack" ? " has been added to " : " has been updated for "} <b>${a.parentApplication.displayName}</div>
        </div>
        <div class="hidden" id="${a.id}">
            
            <table>
                <tr><td>Description</td><td>${a.displayShortDescription.replace("\n", "<br>")}</td></tr>
                <tr><td>Price</td><td>${a.priceFormatted}</td></tr>
                <tr><td>Included DLCs</td><td>${included}</td></tr>
                <tr><td>Parent Application</td><td>${FormatParentApplication(a.parentApplication, a.__id)}</td></tr>
                <tr><td>Id</td><td>${a.id}</td></tr>
                <tr><td>Activity id</td><td>${a.__id}</td></tr>
            </table>
        </div>
    </div>
    <div>
        <input type="button" value="Download" onclick="window.open('https://securecdn.oculus.com/binaries/download/?id=${dlc.id}', '_blank')">
    </div>
</div>`
}

function GetHeadsetName(headset) {
    switch (headset)
    {
        case "RIFT":
            return "Rift";
        case "LAGUNA":
            return "Rift S";
        case "MONTEREY":
            return "Quest 1";
        case "HOLLYWOOD":
            return "Quest 2";
        case "GEARVR":
            return "GearVR";
        case "PACIFIC":
            return "Go";
        default:
            return "unknown";
    }
}

function GetHeadsets(list) {
    var names = []
    list.forEach(x => names.push(GetHeadsetName(x)))
    return names.join(", ")
}

function FormatParentApplication(a, activityId) {
    return `<div class="application">
    <div>
        <div class="flex">
            <div style="padding: 15px; font-weight: bold; color: var(--highlightedColor);" onclick="RevealDescription('${activityId}_${a.id}')" id="${activityId}_${a.id}_trigger" class="anim noselect">&gt;</div>
            <div stlye="font-size: 1.25em;">${a.displayName}</div>
        </div>
        <div class="hidden" id="${activityId}_${a.id}">
            <table>
                <tr><td>Canonical name</td><td>${a.canonicalName}</td></tr>
                <tr><td>Id</td><td>${a.id}</td></tr>
            </table>
        </div>
    </div>
    <div>
        <input type="button" value="Details" onclick="OpenApplication('${a.id}')">
    </div>
</div>`
}

function FormatApplicationActivity(a) {
    return `<div class="application">
    <div>
        <div class="flex">
            <div style="padding: 15px; font-weight: bold; color: var(--highlightedColor);" onclick="RevealDescription('${a.__id}')" id="${a.__id}_trigger" class="anim noselect">&gt;</div>
            <div stlye="font-size: 1.25em;">New Application released! <b>${a.displayName}</b></div>
        </div>
        <div class="hidden" id="${a.__id}">
            <table>
                <tr><td>Description</td><td>${a.displayLongDescription.replace("\n", "<br>")}</td></tr>
                <tr><td>Price</td><td>${a.priceFormatted}</td></tr>
                <tr><td>Supported Headsets</td><td>${GetHeadsets(a.supportedHmdPlatforms)}</td></tr>
                <tr><td>Publisher</td><td>${a.publisherName}</td></tr>
                <tr><td>Release time</td><td>${new Date(a.releaseDate).toLocaleString()}</td></tr>
                <tr><td>Application id</td><td>${a.id}</td></tr>
                <tr><td>Activity id</td><td>${a.__id}</td></tr>
            </table>
        </div>
    </div>
    <div>
        <input type="button" value="Details" onclick="OpenActivity('${a.__id}')">
    </div>
</div>`
}

function FormatPriceChanged(a) {
    return `<div class="application">
    <div>
        <div class="flex">
            <div style="padding: 15px; font-weight: bold; color: var(--highlightedColor);" onclick="RevealDescription('${a.__id}')" id="${a.__id}_trigger" class="anim noselect">&gt;</div>
            <div stlye="font-size: 1.25em;">Price of <b>${a.parentApplication.displayName}</b> changed from ${a.oldPriceFormatted} to <b>${a.newPriceFormatted}</b></div>
        </div>
        <div class="hidden" id="${a.__id}">
            <table>
                <tr><td>New Price</td><td>${a.newPriceFormatted}</td></tr>
                <tr><td>Old Price</td><td>${a.oldPriceFormatted}</td></tr>
                <tr><td>Parent Application</td><td>${FormatParentApplication(a.parentApplication, a.__id)}</td></tr>
                <tr><td>Activity id</td><td>${a.__id}</td></tr>
            </table>
        </div>
    </div>
    <div>
        <input type="button" value="Details" onclick="OpenActivity('${a.__id}')">
    </div>
</div>`
}

function AutoFormat(e, connected, htmlid = "") {
    if(e.__OculusDBType == "Application") return FormatApplication(e, htmlid)
    if(e.__OculusDBType == "IAPItem") return FormatDLC(e)
    if(e.__OculusDBType == "IAPItemPack") return FormatDLCPack(e, connected.dlcs)
    if(e.__OculusDBType == "Version") return FormatVersion(e)
    if(e.__OculusDBType == "ActivityNewDLC" || e.__OculusDBType == "ActivityDLCUpdated") return FormatDLCActivity(e)
    if(e.__OculusDBType == "ActivityNewDLCPack" || e.__OculusDBType == "ActivityDLCPackUpdated") return FormatDLCPackActivity(e)
    if(e.__OculusDBType == "ActivityNewVersion" || e.__OculusDBType == "ActivityVersionUpdated") return FormatVersionActivity(e)
    if(e.__OculusDBType == "ActivityNewApplication") return FormatApplicationActivity(e)
    if(e.__OculusDBType == "ActivityPriceChanged") return FormatPriceChanged(e)
    return ""
}

function GetDLC(dlcs, id) {
    return dlcs.filter(x => x.id == id)[0]
}

function FormatVersionActivity(v) {
    var releaseChannels = ""
    v.releaseChannels.forEach(x => {
        releaseChannels += `${x.channel_name}, `
    })
    if(releaseChannels.length > 0) releaseChannels = releaseChannels.substring(0, releaseChannels.length - 2)
    var downloadable = releaseChannels != ""
    return `<div class="application">
    <div>
        <div class="flex">
            <div style="padding: 15px; font-weight: bold; color: var(--highlightedColor);" onclick="RevealDescription('${v.id}')" id="${v.id}_trigger" class="anim noselect">&gt;</div>
            <div stlye="font-size: 1.25em;">Version <b>${v.version} &nbsp;&nbsp;&nbsp;&nbsp;(${v.versionCode})</b> of <b>${v.parentApplication.displayName}</b> ${v.__OculusDBType == "ActivityVersionUpdated" ? `has been updated` : `has been uploaded`}</div>
        </div>
        <div class="hidden" id="${v.id}">
            <table>
                <tr><td>Uploaded</td><td>${new Date(v.uploadedTime).toLocaleString()}</td></tr>
                <tr><td>Release Channels</td><td>${downloadable ? releaseChannels : "none"}</td></tr>
                <tr><td>Downloadable</td><td>${downloadable}</td></tr>
                <tr><td>Version</td><td>${v.version}</td></tr>
                <tr><td>Version code</td><td>${v.versionCode}</td></tr>
                <tr><td>Parent Application</td><td>${FormatParentApplication(v.parentApplication, v.__id)}</td></tr>
                <tr><td>Id</td><td>${v.id}</td></tr>
                <tr><td>Activity id</td><td>${v.__id}</td></tr>
            </table>
        </div>
    </div>
    <div>
        <input type="button" value="Details" onclick="OpenActivity(${v.__id})">
        <input type="button" value="Download${downloadable ? '"' : ' (Developer only)" class="red"'} onclick="window.open('https://securecdn.oculus.com/binaries/download/?id=${v.id}', '_blank')">
    </div>
</div>`
}

function FormatVersion(v) {
    var releaseChannels = ""
    v.binary_release_channels.nodes.forEach(x => {
        releaseChannels += `${x.channel_name}, `
    })
    if(releaseChannels.length > 0) releaseChannels = releaseChannels.substring(0, releaseChannels.length - 2)
    var downloadable = releaseChannels != ""
    return `<div class="application">
    <div>
        <div class="flex">
            <div style="padding: 15px; font-weight: bold; color: var(--highlightedColor);" onclick="RevealDescription('${v.id}')" id="${v.id}_trigger" class="anim noselect">&gt;</div>
            <div stlye="font-size: 1.25em;">${v.version} &nbsp;&nbsp;&nbsp;&nbsp;(${v.versionCode})</div>
        </div>
        <div class="hidden" id="${v.id}">
            <table>
                <tr><td>Uploaded</td><td>${new Date(v.created_date * 1000).toLocaleString()}</td></tr>
                <tr><td>Release Channels</td><td>${downloadable ? releaseChannels : "none"}</td></tr>
                <tr><td>Downloadable</td><td>${downloadable}</td></tr>
                <tr><td>Version</td><td>${v.version}</td></tr>
                <tr><td>Version code</td><td>${v.versionCode}</td></tr>
                <tr><td>Id</td><td>${v.id}</td></tr>
            </table>
        </div>
    </div>
    <div>
        <input type="button" value="Download${downloadable ? '"' : ' (Developer only)" class="red"'} onclick="window.open('${v.uri}', '_blank')">
    </div>
</div>`
}

function RevealDescription(id) {
    var elem = document.getElementById(id)
    if(elem.className.includes("hidden")) {
        elem.classList.remove("hidden")
    } else {
        elem.classList.add("hidden")
    }
    var trigger = document.getElementById(`${id}_trigger`)
    if(trigger.className.includes("rotate")) {
        trigger.classList.remove("rotate")
    } else {
        trigger.classList.add("rotate")
    }
}


// Local convenience stuff
var script = document.createElement("script")
script.src = "/debug.js"
document.head.appendChild(script)