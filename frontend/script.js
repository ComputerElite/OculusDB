document.head.innerHTML += `<link href="https://fonts.googleapis.com/css?family=Open+Sans:400,400italic,700,700italic" rel="stylesheet" type="text/css">^`

document.body.innerHTML = document.body.innerHTML + `<div class="navBar">
<div class="navBarInnerLeft websitename anim" style="cursor: pointer;" onclick="window.location.href = '/'">
    <img class="navBarElement" src="https://computerelite.github.io/assets/CE_512px.png" style="height: 100%;">
    <div class="navBarElement title anim">
        OculusDB
    </div>
</div>
<div id="navBarToggle" class="navBarToggle anim">
    <div class="navBarPart"></div>
    <div class="navBarPart"></div>
    <div class="navBarPartBottom"></div>
</div>
<div id="navBarContent" class="navBarInnerRight anim">
    <div class="navBarElement">
        <input type="text" placeholder="Query" id="query">
        <input type="button" onclick="Search('query')" value="Search">
    </div>
    <a class="underlineAnimation navBarElement" href="/login">Login</a>
    <a class="underlineAnimation navBarElement" href="/privacy">Privacy Policy</a>
    <a class="underlineAnimation navBarElement" href="/recentactivity">Recent activity</a>
    <a class="underlineAnimation navBarElement" href="/server">Server</a>
    <a class="underlineAnimation navBarElement" href="/downloadstats">Download stats</a>
    <a class="underlineAnimation navBarElement" href="/guide">Downgrading guide</a>
</div>
</div>`

var navBarOpen = false
document.getElementById("navBarToggle").onclick = e => {
    navBarOpen = !navBarOpen
    document.getElementById("navBarToggle").classList.toggle("rotate", navBarOpen)
    document.getElementById("navBarContent").classList.toggle("visible", navBarOpen)
}

const loader = `<div class="centerIt">
<div class="loader"></div>
</div>`
const noResult = `
<div class="application centerIt" style="cursor: default;">
    <b>No Results</b>
</div>
`
const noActivity = `
<div class="application centerIt" style="cursor: default;">
    <b>No Activity</b>
</div>
`

const noDownloads = `
<div class="application centerIt" style="cursor: default;">
    <b>No download on this page</b>
</div>
`

const contextMenu = `
<div class="contextMenu" id="contextMenu">

</div>`

var newTab = false

// context menu
function UpdateContextMenu() {
    console.log("registering context menu events")
    Array.prototype.forEach.call(document.getElementsByClassName("contextmenuenabled"), (e) => {
        
        e.oncontextmenu = ContextMenuEnabled
    })
}

function Copy(text) {
    navigator.clipboard.writeText(text)
}

var contextMenuOpened = Date.now()
var opened = false

document.onclick = e => {
    ClearContextMenu()
}

function ClearContextMenu() {
    var oldMenu = document.getElementById("contextMenu")
    if(oldMenu) {
        oldMenu.remove()
    }
}

function ContextMenuEnabled(event, initiator) {
    if(Date.now() - contextMenuOpened < 500 && opened) return
    opened = true
    contextMenuOpened = Date.now()
    event.preventDefault()
    
    ClearContextMenu()
    var wrapper = document.createElement("div")
    wrapper.innerHTML = contextMenu
    document.body.appendChild(wrapper.firstElementChild)
    var menu = document.getElementById("contextMenu")
    menu.style.top = event.pageY + "px"
    menu.style.left= event.pageX + "px"

    i = 0
    var currentOption
    while(currentOption = initiator.getAttribute(`cmov-${i}`)) {
        menu.innerHTML += `
        <div onclick="${currentOption};ClearContextMenu()" class="contextMenuElement anim">
            ${initiator.getAttribute(`cmon-${i}`)}
        </div>
        `
        i++
    }
    while(parseInt(menu.style.left.replace("px", "")) + menu.offsetWidth >= window.innerWidth - 30) {
        menu.style.left = (parseInt(menu.style.left.replace("px", "")) - 10) + "px"
    }
}

function SetCheckboxesBasedOnValue(options, value) {
    if(value != undefined) {
        var split = value.split(",")
        for (const [key, value] of Object.entries(options)) {
            split.forEach(x => {
                if(value.includes(x)) document.getElementById(key).checked = true
            })
        }
    } else {
        for (const [key, value] of Object.entries(options)) {
            document.getElementById(key).checked = true
        }
        Update(false)
    }
}

function GetValuesOFCheckboxes(options) {
    var filter = []

    for (const [key, value] of Object.entries(options)) {
        if(document.getElementById(key).checked) {
            value.forEach(x => {
                filter.push(x)
            })
        }
    }

    return filter.join(",")
}

function PopUp(html) {
    var popup = document.getElementById("popup")
    if(popup) popup.remove();
    document.body.innerHTML+= `
        <div class="centerIt popUp" id="popup" onclick="ClosePopUp(event)"><div class="popUpContent">${html}</div></div>
    `
}

function ClosePopUp(e = {target: {id: "popup"}}) {
    if(e.target.id == "popup") {
        document.getElementById("popup").remove()
    }
}

function IsHeadsetAndroid(h) {
    if(h == 0 || h == 5) return false;
    return true
}

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
    evt.className += " active";
  }

document.getElementById("query").onkeydown = e => {
    if(e.code == "Enter") {
        Search("query")
    }
}
const params = new URLSearchParams(window.location.search)
if(params.get("isqavs")) localStorage.isQAVS = "true"
// Add analytics
if(!localStorage.isQAVS) {
    var script = document.createElement("script")
    script.src = "https://analytics.rui2015.me/analytics.js?origin=" + location.origin
    document.head.appendChild(script)
}

function GetObjectById(id) {
    return new Promise((resolve, reject) => {
        fetch("/api/v1/id/" + id).then(res => {
            if(res.status != 200) reject(res.status)
            res.json().then(res => resolve(res))
        })
    })
    
}

function GetActivityById(id) {
    return new Promise((resolve, reject) => {
        fetch("/api/v1/activityid/" + id).then(res => {
            if(res.status != 200) reject(res.status)
            res.json().then(res => resolve(res))
        })
    })
    
}

function Search(element)
{
    var query = document.getElementById(element).value
    if(params.get("headsets")) {
        query += "&headsets=" + params.get("headsets")
    }
    window.location = "/search?query=" + query
}

function OpenLocation(tar) {
    if(newTab) {
        window.open(tar, "_blank").focus()
    } else {
        window.location = tar
    }
}

function GetIdLink(id) {
    return location.origin + '/id/' + id
}

function GetActivityLink(id) {
    return location.origin + '/activity/' + id
}

function OpenApplication(id) {
    OpenLocation(GetIdLink(id))
}

function OpenActivity(id) {
    OpenLocation(GetActivityLink(id))
}

function GetOculusLink(id, hmd) {
    var link = "https://www.oculus.com/experiences/"
    if(hmd == 0 || hmd == 5) link += "rift"
    else if(hmd == 1 ||hmd == 2) link += "quest"
    else if(hmd == 3) link += "gear-vr"
    else if(hmd == 4) link += "go"
    return link + "/" + id
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

function GetHeadsetNameEnum(headset) {
    switch (headset)
    {
        case 0:
            return "Rift";
        case 5:
            return "Rift S";
        case 1:
            return "Quest 1";
        case 2:
            return "Quest 2";
        case 3:
            return "GearVR";
        case 4:
            return "Go";
        default:
            return "unknown";
    }
}

function GetLogicalHeadsetNameEnum(headset) {
    switch (headset)
    {
        case 0:
            return "Rift and Rift S";
        case 5:
            return "Rift and Rift S";
        case 1:
            return "Quest 1 and Quest 2";
        case 2:
            return "Quest 1 and Quest 2";
        case 3:
            return "GearVR";
        case 4:
            return "Go";
        default:
            return "unknown";
    }
}

function GetLogicalHeadsetCodeNameEnum(headset) {
    switch (headset)
    {
        case "0":
            return "RIFT";
        case "1":
            return "MONTEREY";
        case "2":
            return "MONTEREY";
        case "3":
            return "GEARVR";
        case "4":
            return "PACIFIC";
        case "5":
            return "RIFT";
        default:
            return "unknown";
    }
}

function GetHeadsetNameOD(headset) {
    switch (headset)
    {
        case "RIFT":
            return "Rift";
        case "LAGUNA":
            return "Rift";
        case "MONTEREY":
            return "Quest";
        case "HOLLYWOOD":
            return "Quest";
        case "GEARVR":
            return "GearVR";
        case "PACIFIC":
            return "Go";
        default:
            return "unknown";
    }
}

function SendDataToParent(data) {
    window.top.postMessage(data, "*")
}

function GetHeadsets(list) {
    var names = []
    list.forEach(x => names.push(GetHeadsetName(x)))
    return names.join(", ")
}

function GetChangelog(version) {
    if(version.changeLog == null) {
        return "We are working on getting the changelog for you. Please check again in a few minutes. This may take longer depending on what's to do. Thanks!"
    } else if(version.changeLog) {
        return version.changeLog.replace(/\</, "&lt;").replace(/\>/, "&gt;").replace(/\n/, "<br>")
    } else {
        return "No changes documented"
    }
}

function GetCollapsableInfo(title, collapsed, htmlid) {
    return `<div class="application">
    <div class="info">
        <div class="flex outside">
            <div class="flex header" onclick="RevealDescription('${htmlid}')">
                <div style="padding: 15px; font-weight: bold; color: var(--highlightedColor);" id="${htmlid}_trigger" class="anim noselect">&gt;</div>
                <div stlye="font-size: 1.25em;">${title}</div>
            </div>
        </div>

        <div class="hidden" id="${htmlid}">
            ${collapsed}
        </div>
    </div>
</div>`
}

function FormatDLC(dlc, htmlid = "") {
    if(htmlid == "") htmlid = dlc.id
    return `<div class="application" oncontextmenu="ContextMenuEnabled(event, this)" cmon-0="Copy link" cmov-0="Copy(GetIdLink('${dlc.id}'))">
    <div class="info">
        <div class="flex outside">
            <div class="buttons">
                <input type="button" value="Download" onmousedown="MouseDown(event)" onmouseup="if(MouseUp(event)) DownloadID('${dlc.latestAssetFileId}')">
            </div>
            <div class="flex header" onclick="RevealDescription('${htmlid}')">
                <div style="padding: 15px; font-weight: bold; color: var(--highlightedColor);" id="${htmlid}_trigger" class="anim noselect">&gt;</div>
                <div stlye="font-size: 1.25em;">${dlc.display_name}</div>
            </div>
        </div>

        <div class="hidden" id="${htmlid}">
            
            <table>
                <colgroup>
                    <col width="220em">
                    <col width="100%">
                </colgroup>
                ${dlc.downloads ? `<tr><td class="label">Downloads</td><td class="value">${dlc.downloads}</td></tr>` : ""}
                <tr><td class="label">Description</td><td class="value">${dlc.display_short_description.replace("\n", "<br>")}</td></tr>
                <tr><td class="label">Price</td><td class="value">${dlc.current_offer.price.formatted}</td></tr>
                <tr><td class="label">latest asset file id</td><td class="value">${dlc.latestAssetFileId}</td></tr>
                <tr><td class="label">Id</td><td class="value">${dlc.id}</td></tr>
            </table>
        </div>
    </div>
</div>`
}

function FormatDLCPack(dlc, dlcs, htmlid = "") {
    var included = ""
    dlc.bundle_items.forEach(d => {
        included += FormatDLC(GetDLC(dlcs, d.id), htmlid + "_" + d.id)
    })
    return `<div class="application" oncontextmenu="ContextMenuEnabled(event, this)" cmon-0="Copy link" cmov-0="Copy(GetIdLink('${dlc.id}'))">
    <div class="info">
        <div class="flex outside">
            <div class="buttons">
                <input type="button" value="Download" onmousedown="MouseDown(event)" onmouseup="if(MouseUp(event)) DownloadID('${dlc.id}')">
            </div>
            <div class="flex header" onclick="RevealDescription('${dlc.id}')">
                <div style="padding: 15px; font-weight: bold; color: var(--highlightedColor);" id="${htmlid}_trigger" class="anim noselect">&gt;</div>
                <div stlye="font-size: 1.25em;">${dlc.display_name}</div>
            </div>
        </div>

        <div class="hidden" id="${htmlid}">
            
            <table>
                <colgroup>
                    <col width="150em">
                    <col width="100%">
                </colgroup>
                ${dlc.downloads ? `<tr><td class="label">Downloads</td><td class="value">${dlc.downloads}</td></tr>` : ""}
                <tr><td class="label">Description</td><td class="value">${dlc.display_short_description.replace("\n", "<br>")}</td></tr>
                <tr><td class="label">Price</td><td class="value">${dlc.current_offer.price.formatted}</td></tr>
                <tr><td class="label">Included DLCs</td><td class="value">${included}</td></tr>
                <tr><td class="label">Id</td><td class="value">${dlc.id}</td></tr>
            </table>
        </div>
    </div>
    
</div>`
}

function MouseUp(e) {
    if (e.button === 1) {
        e.preventDefault()
        newTab = true
    } else {
        newTab = false;
    }
    if(e.button == 2) return false
    return true
}

function MouseDown(e) {
    if (e.button === 1) {
        e.preventDefault()
    }
}

function FormatDLCActivity(a, htmlid) {
    return `<div class="application" oncontextmenu="ContextMenuEnabled(event, this)" cmon-0="Copy activity link" cmov-0="Copy(GetActivityLink('${a.__id}'))">
    <div class="info">
        <div class="flex outside">
            <div class="buttons">
                <input type="button" value="Details" onmousedown="MouseDown(event)" onmouseup="if(MouseUp(event)) OpenActivity('${a.__id}')">
            </div>
            <div class="flex header" onclick="RevealDescription('${htmlid}')">
                <div>${GetTimeString(a.__lastUpdated)}</div>
                <div style="padding: 15px; font-weight: bold; color: var(--highlightedColor);" id="${htmlid}_trigger" class="anim noselect">&gt;</div>
                <div stlye="font-size: 1.25em;">DLC <b>${a.displayName}</b> ${a.__OculusDBType == "ActivityNewDLC" ? " has been added to " : " has been updated for "} <b>${a.parentApplication.displayName}</b></div>
            </div>
        </div>

        <div class="hidden" id="${htmlid}">
            <table>
                <colgroup>
                    <col width="200em">
                    <col width="100%">
                </colgroup>
                <tr><td class="label">Description</td><td class="value">${a.displayShortDescription.replace("\n", "<br>")}</td></tr>
                <tr><td class="label">Price</td><td class="value">${a.priceFormatted}</td></tr>
                <tr><td class="label">DLC id</td><td class="value">${a.id}</td></tr>
                <tr><td class="label">Parent Application</td><td class="value">${FormatParentApplication(a.parentApplication, htmlid)}</td></tr>
                <tr><td class="label">Activity id</td><td class="value">${a.__id}</td></tr>
            </table>
        </div>
    </div>
    
</div>`
}

function FormatDLCPackActivityDLC(a, i) {
    return `<div class="application">
    <div class="info">
        <div class="flex outside">
            <div class="buttons">
                <input type="button" value="Details" onmousedown="MouseDown(event)" onmouseup="if(MouseUp(event)) OpenApplication('${a.id}')">
            </div>
            <div class="flex header" onclick="RevealDescription('${i}_${a.__id}')" >
                <div style="padding: 15px; font-weight: bold; color: var(--highlightedColor);" id="${i}_${a.__id}_trigger" class="anim noselect">&gt;</div>
                <div stlye="font-size: 1.25em;"></div>
            </div>
        </div>

        <div class="hidden" id="${i}_${a.__id}">
            <table>
                <colgroup>
                    <col width="130em">
                    <col width="100%">
                </colgroup>
                <tr><td class="label">Description</td><td class="value">${a.displayShortDescription.replace("\n", "<br>")}</td></tr>
                <tr><td class="label">DLC id</td><td class="value">${a.id}</td></tr>
            </table>
        </div>
    </div>
</div>`
}

function FormatDLCPackActivity(a, htmlid) {
    var included = ""
    a.includedDLCs.forEach(d => {
        included += FormatDLCPackActivityDLC(d, a.__id)
    })
    return `<div class="application" oncontextmenu="ContextMenuEnabled(event, this)" cmon-0="Copy activity link" cmov-0="Copy(GetActivityLink('${a.__id}'))">
    <div class="info">
        <div class="flex outside">
            <div class="buttons">
                <input type="button" value="Download" onmousedown="MouseDown(event)" onmouseup="if(MouseUp(event)) DownloadID('${dlc.id}')">
            </div>
            <div class="flex header" onclick="RevealDescription('${htmlid}')">
                <div>${GetTimeString(a.__lastUpdated)}</div>
                <div style="padding: 15px; font-weight: bold; color: var(--highlightedColor);" id="${htmlid}_trigger" class="anim noselect">&gt;</div>
                <div stlye="font-size: 1.25em;">DLC Pack ${a.displayName} ${a.__OculusDBType == "ActivityNewDLCPack" ? " has been added to " : " has been updated for "} <b>${a.parentApplication.displayName}</div>
            </div>
        </div>

        <div class="hidden" id="${htmlid}">
            
            <table>
                <colgroup>
                    <col width="200em">
                    <col width="100%">
                </colgroup>
                <tr><td class="label">Description</td><td class="value">${a.displayShortDescription.replace("\n", "<br>")}</td></tr>
                <tr><td class="label">Price</td><td class="value">${a.priceFormatted}</td></tr>
                <tr><td class="label">Included DLCs</td><td class="value">${included}</td></tr>
                <tr><td class="label">Parent Application</td><td class="value">${FormatParentApplication(a.parentApplication, htmlid)}</td></tr>
                <tr><td class="label">DLC pack id</td><td class="value">${a.id}</td></tr>
                <tr><td class="label">Activity id</td><td class="value">${a.__id}</td></tr>
            </table>
        </div>
    </div>
</div>`
}

function FormatParentApplication(a, activityId) {
    return `<div class="application">
    <div class="info">
        <div class="flex outside">
            <div class="buttons">
                <input type="button" value="Details" onmousedown="MouseDown(event)" onmouseup="if(MouseUp(event)) OpenApplication('${a.id}')">
            </div>
            <div class="flex header" onclick="RevealDescription('${activityId}_${a.id}')">
                <div style="padding: 15px; font-weight: bold; color: var(--highlightedColor);" id="${activityId}_${a.id}_trigger" class="anim noselect">&gt;</div>
                <div stlye="font-size: 1.25em;">${a.displayName}</div>
            </div>
        </div>

        <div class="hidden" id="${activityId}_${a.id}">
            <table>
                <colgroup>
                    <col width="160em">
                    <col width="100%">
                </colgroup>
                <tr><td class="label">Canonical name</td><td class="value">${a.canonicalName}</td></tr>
                <tr><td class="label">Id</td><td class="value">${a.id}</td></tr>
            </table>
        </div>
    </div>
</div>`
}

function FormatApplication(application, htmlId = "") {
    return `<div class="application" oncontextmenu="ContextMenuEnabled(event, this)" cmon-0="Copy link" cmov-0="Copy(GetIdLink('${application.id}'))">
    <div class="info">
        <div class="flex outside">
            <div class="buttons">
                <input type="button" value="Details" onmousedown="MouseDown(event)" onmouseup="if(MouseUp(event)) OpenApplication('${application.id}')">
            </div>
            <div class="flex header" onclick="RevealDescription('${htmlId}')">
                <div style="padding: 15px; font-weight: bold; color: var(--highlightedColor);" id="${htmlId}_trigger" class="anim noselect">&gt;</div>
                <img onerror="this.src = '/notfound.jpg'" src="${application.imageLink}" style="max-height: 4em; width: auto; margin-right: 10px;">
                <div stlye="font-size: 1.25em;">${application.displayName} (${ GetLogicalHeadsetNameEnum(application.hmd).replace(" and ", ", ")})</div>
            </div>
            
        </div>
        <div class="hidden" id="${htmlId}">
            <table>
                <colgroup>
                    <col width="200em">
                    <col width="100%">
                </colgroup>
                <tr><td class="label">Description</td><td class="value">${application.display_long_description.replace("\n", "<br>")}</td></tr>
                <tr><td class="label">Current price</td><td class="value">${application.current_offer.price.formatted}</td></tr>
                <tr><td class="label">Baseline price</td><td class="value">${application.baseline_offer.price.offset_amount != 0 && application.baseline_offer.price.offset_amount < application.current_offer.price.offset_amount ? application.baseline_offer.price.formatted : application.current_offer.price.formatted}</td></tr>
                <tr><td class="label">Rating</td><td class="value">${application.quality_rating_aggregate.toFixed(2)}</td></tr>
                <tr><td class="label">Supported Headsets</td><td class="value">${GetHeadsets(application.supported_hmd_platforms)}</td></tr>
                <tr><td class="label">Publisher</td><td class="value">${application.publisher_name}</td></tr>
                <tr><td class="label">Package name</td><td class="value">${application.packageName ? application.packageName : "Not available"}</td></tr>
                <tr><td class="label">Canonical name</td><td class="value">${application.canonicalName}</td></tr>
                <tr><td class="label">Link to Oculus</td><td class="value"><a href="${GetOculusLink(application.id, application.hmd)}">${GetOculusLink(application.id, application.hmd)}</a></td></tr>
                <tr><td class="label">Id</td><td class="value">${application.id}</td></tr>
            </table>
        </div>
    </div>

</div>`
}

function FormatApplicationActivity(a, htmlid) {
    return `<div class="application" oncontextmenu="ContextMenuEnabled(event, this)" cmon-0="Copy activity link" cmov-0="Copy(GetActivityLink('${a.__id}'))">
    <div class="info">
        <div class="flex outside">
            <div class="buttons">
                <input type="button" value="Details" onmousedown="MouseDown(event)" onmouseup="if(MouseUp(event)) OpenActivity('${a.__id}')">
                <input type="button" value="View Application" onmousedown="MouseDown(event)" onmouseup="if(MouseUp(event)) OpenApplication('${a.id}')">
            </div>
            <div class="flex header" onclick="RevealDescription('${htmlid}')">
                <div>${GetTimeString(a.__lastUpdated)}</div>
                <div style="padding: 15px; font-weight: bold; color: var(--highlightedColor);" id="${htmlid}_trigger" class="anim noselect">&gt;</div>
                <div stlye="font-size: 1.25em;">New Application released! <b>${a.displayName}</b></div>
            </div>
        </div>

        <div class="hidden" id="${htmlid}">
            <table>
                <colgroup>
                    <col width="200em">
                    <col width="100%">
                </colgroup>
                <tr><td class="label">Description</td><td class="value">${a.displayLongDescription.replace("\n", "<br>")}</td></tr>
                <tr><td class="label">Price</td><td class="value">${a.priceFormatted}</td></tr>
                <tr><td class="label">Supported Headsets</td><td class="value">${GetHeadsets(a.supportedHmdPlatforms)}</td></tr>
                <tr><td class="label">Publisher</td><td class="value">${a.publisherName}</td></tr>
                <tr><td class="label">Release time</td><td class="value">${new Date(a.releaseDate).toLocaleString()}</td></tr>
                <tr><td class="label">Link to Oculus</td><td class="value"><a href="${GetOculusLink(a.id, a.hmd)}">${GetOculusLink(a.id, a.hmd)}</a></td></tr>
                <tr><td class="label">Application id</td><td class="value">${a.id}</td></tr>
                <tr><td class="label">Activity id</td><td class="value">${a.__id}</td></tr>
            </table>
        </div>
    </div>
</div>`
}

function FormatPriceChanged(a, htmlid) {
    return `<div class="application" oncontextmenu="ContextMenuEnabled(event, this)" cmon-0="Copy activity link" cmov-0="Copy(GetActivityLink('${a.__id}'))">
    <div class="info">

        <div class="flex outside">
            <div class="buttons">
                <input type="button" value="Details" onmousedown="MouseDown(event)" onmouseup="if(MouseUp(event)) OpenActivity('${a.__id}')">
            </div>
            <div class="flex header" onclick="RevealDescription('${htmlid}')">
                <div>${GetTimeString(a.__lastUpdated)}</div>
                <div style="padding: 15px; font-weight: bold; color: var(--highlightedColor);" id="${htmlid}_trigger" class="anim noselect">&gt;</div>
                <div stlye="font-size: 1.25em;">Price of <b>${a.parentApplication.displayName}</b> changed from ${a.oldPriceFormatted} to <b>${a.newPriceFormatted}</b></div>
            </div>
        </div>

        <div class="hidden" id="${htmlid}">
            <table>
                <colgroup>
                    <col width="200em">
                    <col width="100%">
                </colgroup>
                <tr><td class="label">New Price</td><td class="value">${a.newPriceFormatted}</td></tr>
                <tr><td class="label">Old Price</td><td class="value">${a.oldPriceFormatted}</td></tr>
                <tr><td class="label">Parent Application</td><td class="value">${FormatParentApplication(a.parentApplication, htmlid)}</td></tr>
                <tr><td class="label">Activity id</td><td class="value">${a.__id}</td></tr>
            </table>
        </div>
    </div>
    
</div>`
}

function FormatVersion(v, htmlid = "") {
    var releaseChannels = ""
    v.binary_release_channels.nodes.forEach(x => {
        releaseChannels += `${x.channel_name}, `
    })
    if(releaseChannels.length > 0) releaseChannels = releaseChannels.substring(0, releaseChannels.length - 2)
    var downloadable = releaseChannels != ""
    return `<div class="application" oncontextmenu="ContextMenuEnabled(event, this)" cmon-0="Copy link" cmov-0="Copy(GetIdLink('${v.id}'))">
    <div class="info">
        <div class="flex outside">
            <div class="buttons">
                ${GetDownloadButtonVersion(downloadable, v.id, v.parentApplication.hmd, v.parentApplication, v.version)}
            </div>
            <div class="flex header" onclick="RevealDescription('${htmlid}')">
                <div style="padding: 15px; font-weight: bold; color: var(--highlightedColor);" id="${htmlid}_trigger" class="anim noselect">&gt;</div>
                <div stlye="font-size: 1.25em;">${v.version} &nbsp;&nbsp;&nbsp;&nbsp;(${v.versionCode + (v.downloads ? `; ${v.downloads}` : ``)})</div>
            </div>
            
        </div>

        <div class="hidden" id="${htmlid}">
            <table>
                <colgroup>
                    <col width="180em">
                    <col width="100%">
                </colgroup>
                ${v.downloads ? `<tr><td class="label">Downloads</td><td class="value">${v.downloads}</td></tr>` : ""}
                <tr><td class="label">Uploaded</td><td class="value">${new Date(v.created_date * 1000).toLocaleString()}</td></tr>
                <tr><td class="label">Release Channels</td><td class="value">${downloadable ? releaseChannels : "none"}</td></tr>
                <tr><td class="label">Downloadable</td><td class="value">${downloadable}</td></tr>
                <tr><td class="label">Version</td><td class="value">${v.version}</td></tr>
                <tr><td class="label">Version code</td><td class="value">${v.versionCode}</td></tr>
                <tr><td class="label">Changelog</td><td class="value">${GetChangelog(v)}</td></tr>
                <tr><td class="label">Id</td><td class="value">${v.id}</td></tr>
            </table>
        </div>
    </div>
</div>`
}

function FormatVersionActivity(v, htmlid) {
    var releaseChannels = ""
    v.releaseChannels.forEach(x => {
        releaseChannels += `${x.channel_name}, `
    })
    if(releaseChannels.length > 0) releaseChannels = releaseChannels.substring(0, releaseChannels.length - 2)
    var downloadable = releaseChannels != ""
    return `<div class="application" oncontextmenu="ContextMenuEnabled(event, this)" cmon-0="Copy activity link" cmov-0="Copy(GetActivityLink('${v.__id}'))">
    <div class="info">
        <div class="flex outside">
            <div class="buttons">
                <input type="button" value="Details" onmousedown="MouseDown(event)" onmouseup="if(MouseUp(event)) OpenActivity('${v.__id}')">
                ${GetDownloadButtonVersion(downloadable, v.id, v.parentApplication.hmd, v.parentApplication, v.version)}
            </div>
            <div class="flex header" onclick="RevealDescription('${htmlid}')">
                <div>${GetTimeString(v.__lastUpdated)}</div>
                <div style="padding: 15px; font-weight: bold; color: var(--highlightedColor);" id="${htmlid}_trigger" class="anim noselect">&gt;</div>
                <div stlye="font-size: 1.25em;">Version <b>${v.version} &nbsp;&nbsp;&nbsp;&nbsp;(${v.versionCode})</b> of <b>${v.parentApplication.displayName}</b> ${v.__OculusDBType == "ActivityVersionUpdated" ? `has been updated` : `has been uploaded`}</div>
            </div>
        </div>
        
        <div class="hidden" id="${htmlid}">
            <table>
                <colgroup>
                    <col width="200em">
                    <col width="100%">
                </colgroup>
                <tr><td class="label">Uploaded</td><td class="value">${new Date(v.uploadedTime).toLocaleString()}</td></tr>
                <tr><td class="label">Release Channels</td><td class="value">${downloadable ? releaseChannels : "none"}</td></tr>
                <tr><td class="label">Downloadable</td><td class="value">${downloadable}</td></tr>
                <tr><td class="label">Version</td><td class="value">${v.version}</td></tr>
                <tr><td class="label">Version code</td><td class="value">${v.versionCode}</td></tr>
                <tr><td class="label">Changelog</td><td class="value">${GetChangelog(v)}</td></tr>
                <tr><td class="label">Parent Application</td><td class="value">${FormatParentApplication(v.parentApplication, htmlid)}</td></tr>
                <tr><td class="label">Id</td><td class="value">${v.id}</td></tr>
                <tr><td class="label">Activity id</td><td class="value">${v.__id}</td></tr>
            </table>
        </div>
    </div>
</div>`
}

function AutoFormat(e, connected, htmlid = "") {
    if(e.__OculusDBType == "Application") return FormatApplication(e, htmlid)
    if(e.__OculusDBType == "IAPItem") return FormatDLC(e, htmlid)
    if(e.__OculusDBType == "IAPItemPack") return FormatDLCPack(e, connected.dlcs, htmlid)
    if(e.__OculusDBType == "Version") return FormatVersion(e, htmlid)
    if(e.__OculusDBType == "ActivityNewDLC" || e.__OculusDBType == "ActivityDLCUpdated") return FormatDLCActivity(e, htmlid)
    if(e.__OculusDBType == "ActivityNewDLCPack" || e.__OculusDBType == "ActivityDLCPackUpdated") return FormatDLCPackActivity(e, htmlid)
    if(e.__OculusDBType == "ActivityNewVersion" || e.__OculusDBType == "ActivityVersionUpdated") return FormatVersionActivity(e, htmlid)
    if(e.__OculusDBType == "ActivityNewApplication") return FormatApplicationActivity(e, htmlid)
    if(e.__OculusDBType == "ActivityPriceChanged") return FormatPriceChanged(e, htmlid)
    return ""
}

function GetDLC(dlcs, id) {
    return dlcs.filter(x => x.id == id)[0]
}

function GetDownloadLink(id) {
    return `https://securecdn.oculus.com/binaries/download/?id=${id}`
}

var sendToParent = InIframe()

function InIframe () {
    try {
        return window.self !== window.top;
    } catch (e) {
        return true;
    }
}

function DownloadID(id) {
    window.open(GetDownloadLink(id), "_blank")
    fetch("/api/v1/reportdownload?time=" + Date.now(), {
        method: "POST",
        body: JSON.stringify({
            id: id
        })
    })
}

function AndroidDownload(id, parentApplicationId,parentApplicationName, version) {
    if(sendToParent) {
        SendDataToParent(JSON.stringify({
            type: "Download",
            binaryId: id,
            parentId: parentApplicationId,
            parentName: parentApplicationName,
            version: version
        }))
    } else {
        if(localStorage.fuckpopups) {
            DownloadID(id)
        } else {
            PopUp(`
            <div>
                To download games you must be logged in on <a href="{oculusloginlink}">{oculusloginlink}</a>. If you aren't logged in you won't be able to download games.
                <br>
                <a onclick="localStorage.fuckpopups = 'yummy, spaghetti'; window.open(GetDownloadLink('${id}')); ClosePopUp();"><i style="cursor: pointer;">Don't show warning again</i></a>
                <div>
                    <input type="button" value="Log in" onmousedown="MouseDown(event)" onmouseup="if(MouseUp(event)) window.open('{oculusloginlink}', )">
                    <input type="button" value="Download" onmousedown="MouseDown(event)" onmouseup="if(MouseUp(event)) { window.open(GetDownloadLink('${id}')); ClosePopUp(); }">
                </div>
            </div>
        `)
        }
    }
}

function GetDownloadButtonVersion(downloadable, id, hmd, parentApplication, version) {
    if(IsHeadsetAndroid(hmd)) {
        return `<input type="button" value="Download${downloadable ? '"' : ' (Developer only)" class="red"'} onmousedown="MouseDown(event)" onmouseup="if(MouseUp(event)) AndroidDownload('${id}', '${parentApplication.id}', '${parentApplication.displayName}', '${version}')" oncontextmenu="ContextMenuEnabled(event, this)" cmon-0="Copy download url" cmov-0="Copy(GetDownloadLink('${id}'))" cmon-1="Show Oculus Downgrader code" cmov-1="AndroidDownloadPopUp('${parentApplication.id}','${id}', '${hmd}')">`
    }
    return `<input type="button" value="Download${downloadable ? '"' : ' (Developer only)" class="red"'} onmousedown="MouseDown(event)" onmouseup="if(MouseUp(event)) RiftDownloadPopUp('${parentApplication.id}','${id}')" oncontextmenu="ContextMenuEnabled(event, this)" cmon-0="Show Oculus Downgrader code" cmov-0="RiftDownloadPopUp('${parentApplication.id}','${id}')">`
}

function RiftDownloadPopUp(appid, versionid) {
    PopUp(`
        <div>
            <b>Rift apps can not be downloaded via this website. To download Rift apps follow the following instructions:</b>
            <br>
            <b>1. </b> Setup Oculus Downgrader by following <a href="https://computerelite.github.io/tools/Oculus/OculusDowngraderGuide.html">these instructions</a>
            <br>
            <b>2. </b> Using Option 11 in Oculus Downgraders main menu enter your password and afterwards paste <code>d --appid ${appid} --versionid ${versionid} --headset rift</code> into Oculus Downgrader and hit enter. The download will start and the app get launched afterwards.
            <br>
            <br>
            <b>Like automation? </b> Use <code>"Oculus Downgrader.exe" -nU d --appid ${appid} --versionid ${versionid} --headset rift</code> to download the version with one command
            <br>
            <br>
            <i>To close this pop up click next to it</i>
        </div>
    `)
}

function AndroidDownloadPopUp(appid, versionid, hmd) {
    PopUp(`
        <div>
            <b>To download apps via Oculus Downgrader follow the following instructions:</b>
            <br>
            <b>1. </b> Setup Oculus Downgrader by following <a href="https://computerelite.github.io/tools/Oculus/OculusDowngraderGuide.html">these instructions</a>
            <br>
            <b>2. Select the right headset via option 8</b>
            <br>
            <b>3. </b> Using Option 11 in Oculus Downgraders main menu enter your password and afterwards paste <code>d --appid ${appid} --versionid ${versionid} --headset ${GetLogicalHeadsetCodeNameEnum(hmd)}</code> into Oculus Downgrader and hit enter. The download will start and the app get installed afterwards. Make sure that your headset is connected via USB.
            <br>
            <br>
            <b>Like automation? </b> Use <code>"Oculus Downgrader.exe" -nU d --appid ${appid} --versionid ${versionid} --headset ${GetLogicalHeadsetCodeNameEnum(hmd)}</code> to download the version with one command
            <br>
            <br>
            <i>To close this pop up click next to it</i>
        </div>
    `)
}



function GetTimeString(d) {
    var date = new Date(d)
    if(date.toLocaleDateString() == new Date(Date.now()).toLocaleDateString()) {
        return date.toLocaleTimeString()
    }
    return date.toLocaleString()
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

function TextBoxError(id, text) {
    ChangeTextBoxProperty(id, "var(--red)", text)
}

function TextBoxText(id, text) {
    ChangeTextBoxProperty(id, "var(--highlightedColor)", text)
}

function TextBoxGood(id, text) {
    ChangeTextBoxProperty(id, "var(--textColor)", text)
}

function HideTextBox(id) {
    document.getElementById(id).style.visibility = "hidden"
}

function ChangeTextBoxProperty(id, color, innerHtml) {
    var text = document.getElementById(id)
    text.style.visibility = "visible"
    text.style.border = color + " 1px solid"
    text.innerHTML = innerHtml
}

function GetCookie(cookieName) {
    var name = cookieName + "=";
    var ca = document.cookie.split(';');
    for (var i = 0; i < ca.length; i++) {
        var c = ca[i];
        while (c.charAt(0) == ' ') {
            c = c.substring(1);
        }
        if (c.indexOf(name) == 0) {
            return c.substring(name.length, c.length);
        }
    }
    return "";
}

function SetCookie(name, value, expiration) {
    var d = new Date();
    d.setTime(d.getTime() + (expiration * 24 * 60 * 60 * 1000));
    var expires = "expires=" + d.toUTCString();
    document.cookie = name + "=" + value + ";" + expires + ";path=/";
}