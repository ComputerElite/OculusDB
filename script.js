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

function Search(element)
{
    var query = document.getElementById(element).value
    window.location = "/search?query=" + query
}

function OpenApplication(id) {
    window.location = "/id/" + id
}

function FormatApplication(application) {
    return `<div class="application">
    <div>
        <div class="flex">
            <div style="padding: 15px; font-weight: bold; color: var(--highlightedColor);" onclick="RevealDescription('${application.id}')" id="${application.id}_trigger" class="anim noselect">&gt;</div>
            <div stlye="font-size: 1.25em;">${application.displayName}</div>
        </div>
        <div class="hidden" id="${application.id}">
            
            <table>
                <tr><td>Description</td><td>${application.display_long_description.replace("\n", "<br>")}</td></tr>
                <tr><td>Current price</td><td>${application.current_offer.price.formatted}</td></tr>
                <tr><td>Baseline price</td><td>${application.baseline_offer.price.offset_amount != 0 && application.baseline_offer.price.offset_amount < application.current_offer.price.offset_amount ? application.baseline_offer.price.formatted : application.current_offer.price.formatted}</td></tr>
                <tr><td>Rating</td><td>${application.quality_rating_aggregate.toFixed(2)}</td></tr>
                <tr><td>id</td><td>${application.id}</td></tr>
            </table>
        </div>
    </div>
    <div>
        <input type="button" value="Explore" onclick="OpenApplication('${application.id}')">
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
                <tr><td>id</td><td>${dlc.id}</td></tr>
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
                <tr><td>id</td><td>${dlc.id}</td></tr>
            </table>
        </div>
    </div>
    <div>
        <input type="button" value="Download" onclick="window.open('https://securecdn.oculus.com/binaries/download/?id=${dlc.id}', '_blank')">
    </div>
</div>`
}

function AutoFormat(e, connected) {
    if(e.__OculusDBType == "Application") return FormatApplication(e)
    if(e.__OculusDBType == "IAPItem") return FormatDLC(e)
    if(e.__OculusDBType == "IAPItemPack") return FormatDLCPack(e, connected.dlcs)
    if(e.__OculusDBType == "Version") return FormatVersion(e)
}

function GetDLC(dlcs, id) {
    return dlcs.filter(x => x.id == id)[0]
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
                <tr><td>id</td><td>${v.id}</td></tr>
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