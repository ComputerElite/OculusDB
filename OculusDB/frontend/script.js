var jokeconfig = {
    flashbang: false,
    popupad: false,
    dialupdownload: false,
    supportUs: false
}

const params = new URLSearchParams(window.location.search)

document.head.innerHTML += `<link href="https://fonts.googleapis.com/css?family=Open+Sans:400,400italic,700,700italic" rel="stylesheet" type="text/css">`

if(!params.get("nonavbar")) document.body.innerHTML = document.body.innerHTML + `<div class="navBar">
<div class="navBarInnerLeft websitename anim" style="cursor: pointer;" onclick="window.location.href = '/'">
    <img alt="ComputerElite icon. Wooden background featuring a windows logo, oculus logo and a piano" class="navBarElement" src="https://computerelite.github.io/assets/CE_512px.png" style="height: 100%;">
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
    <a class="underlineAnimation navBarElement" href="/saved">Saved apps</a>
    <a class="underlineAnimation navBarElement" href="/privacy">Privacy Policy</a>
    <a class="underlineAnimation navBarElement" href="/recentactivity">Recent activity</a>
    <a class="underlineAnimation navBarElement" href="/server">Server</a>
    <a class="underlineAnimation navBarElement" href="/downloadstats">Download stats</a>
    <a class="underlineAnimation navBarElement" href="/guide">Downgrade guide</a>
    <a class="underlineAnimation navBarElement navBarMarginLeft" href="/supportus">Support us</a>
    <a class="underlineAnimation navBarElement" style="height: 100%;" href="{OculusDBDC}"><img alt="Discord logo" style="height: 100%;" src="/assets/discord.svg"></a>
</div>
</div>
<div class="footer">
    <div>This website is not affiliated with Oculus/Meta VR</div>
</div>`

const loader = `<div class="loader"></div>`

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
//PopUp("OculusDB is currently running some live tests. During these tests, the site may crash or not work as expected. We are sorry for the inconvenience")

function HighlightElement(id, smooth = true) {
    document.getElementById(id).scrollIntoView({
        behavior: smooth ? "smooth" : "auto",
        block: "start"
    })
    if(!smooth) {
        window.scrollBy(0, -80)
    }
    document.getElementById(id).classList.remove("highlight")
    setTimeout(() => {
        document.getElementById(id).classList.add("highlight")
        setTimeout(() => {
            document.getElementById(id).classList.remove("highlight")
        }, 4000);
    }, 0);
    
}

function PlaySound(url) {
    var audio = new Audio(url);
    audio.play();
}

function GetRandomBool(trueChance) {
    return Math.random() * trueChance <= 1 // 1 in x
}

if(params.has("isqavs")) localStorage.isQAVS = "true"
if(params.has("isoculusdowngrader")) localStorage.isOculusDowngrader = "true"

// Add analytics
if(!localStorage.isQAVS) {
    var script = document.createElement("script")
    script.src = "https://analytics.rui2015.me/analytics.js?origin=" + location.origin
    document.head.appendChild(script)

    /// JOKES ///
    const ads = [
        {
            img: "/cdn/BS2.jpg",
            title: "",
            xredirect: "",
            redirect: ""
        }
    ]

    if(GetRandomBool(1000) || params.has("dialupdownload")) jokeconfig.dialupdownload = true
    if(params.has("flashbang")) jokeconfig.flashbang = true
    if(params.has("popupad")) jokeconfig.popupad = true
    if(params.has("supportus")) jokeconfig.supportUs = true

    if(jokeconfig.flashbang) {
        document.body.innerHTML += `<div class="flashbang"></div>`
    }

    if(jokeconfig.popupad) {
        setTimeout(() => {
            var ad = ads[Math.floor(Math.random() * ads.length)]
            PopUp(` <div style="width: 95vw; height: 95vh; position: relative;">
                        ${ad.title ? `<h2>${ad.title}</h2>` : ``}
                        <img alt="Image of ${ad.title} ad" src="${ad.img}" style="width: 100%; height: 100%;" onclick="location = '${ad.redirect ? ad.redirect : `https://computerelite.github.io/redirect?target=self&random`}'">
                        <div style="position: absolute; top: 2px; right: 2px; font-size: 6px; color: var(--red); cursor: pointer" onclick="location = '${ad.xredirect ? ad.xredirect : `https://computerelite.github.io/redirect?target=self&random`}';">X</div>
                    </div>`)
            PlaySound("/cdn/boom.ogg")
        }, 1500);
    }

    if(jokeconfig.supportUs) {
        console.log("e")
        var e = ""
        for(let i = 0; i < 500; i++) {
            e += `<a class="underlineAnimation" style="position: fixed; top: ${Math.random() * 100}vh; left: ${Math.random() * 100}vw;" href="/supportus">Support us</a>`
        }
        document.body.innerHTML += e;
    }
    /// JOKES END

    // add survey
    /*
    document.body.innerHTML += `<div class="leftBottom" id="surveyPopup">
    Mind taking a minute to give feedback on OculusDB and its related programs?
    <input type="button" value="Go to Survey" onclick="window.open('https://forms.gle/CaDYkwFbhTTw7LnNA', '_blank')">
    <input type="button" value="Close popup" onclick="document.getElementById('surveyPopup').remove()">
    </div>`
    */
}

var navBarOpen = false
document.getElementById("navBarToggle").onclick = e => {
    navBarOpen = !navBarOpen
    document.getElementById("navBarToggle").classList.toggle("rotate", navBarOpen)
    document.getElementById("navBarContent").classList.toggle("visible", navBarOpen)
}


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
            document.getElementById(key).checked = localStorage.isQAVS ? key == "monterey" ||  key == "hollywood" || key == "seacliff" || key == "eureka" : true
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
    var p = document.createElement("div")
    p.className = "centerIt popUp"
    p.id = "popup"
    p.onclick = event => {
        ClosePopUp(event)
    }
    p.innerHTML+= `
        <div class="popUpContent">${html}</div>
    `
    document.body.appendChild(p)
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


function GetObjectById(id) {
    return new Promise((resolve, reject) => {
        fetch(`/api/v1/id/${id}?currency=${params.get("currency") ? params.get("currency") : ""}`).then(res => {
            if(res.status != 200) {
                res.text().then(text => {
                    PopUp(text)
                    reject(res.status)
                })
            } else {
                res.json().then(res => resolve(res))
            }
        })
    })
    
}

function GetActivityById(id) {
    return new Promise((resolve, reject) => {
        fetch("/api/v1/activityid/" + id).then(res => {
            if(res.status != 200) {
                res.text().then(text => {
                    PopUp(text)
                    reject(res.status)
                })
            } else {
                res.json().then(res => resolve(res))
            }
        })
    })
    
}

function Search(element)
{
    var query = encodeURIComponent(document.getElementById(element).value)
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

function OpenRecentActivity(id) {
    OpenLocation(location.origin + '/recentactivity?application=' + id)
}

function GetOculusLink(id, hmd) {
    return "https://meta.com/en-gb/experiences/" + id;
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
        case "EUREKA":
            return "Quest 3";
        case "GEARVR":
            return "GearVR";
        case "PACIFIC":
            return "Go";
        case "SEACLIFF":
            return "Quest Pro";
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
        case 7:
            return "Quest 3";
        case 6:
            return "Quest Pro";
        case 3:
            return "GearVR";
        case 4:
            return "Go";
        default:
            return "unknown";
    }
}

function GetHeadsetNames(headsets) {
    var names = []
    headsets.forEach(x => {
        names.push(GetHeadsetName(x))
    })
    return names.join(", ")
}

function GetLogicalHeadsetNameEnum(headset) {
    switch (headset)
    {
        case 0:
            return "Rift and Rift S";
        case 5:
            return "Rift and Rift S";
        case 1:
            return "Quest 1, 2, 3 and Pro";
        case 2:
            return "Quest 1, 2, 3 and Pro";
        case 6:
            return "Quest 1, 2, 3 and Pro";
        case 7:
            return "Quest 1, 2, 3 and Pro";
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
        case "6":
            return "SEACLIFF";
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
        case "SEACLIFF":
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
        return FormatChangelog(version.changeLog)
    } else {
        return "No changes documented"
    }
}

function FormatChangelog(c) {
    if(c == null) return "Mo changelog available"
    return c.replace(/\</, "&lt;").replace(/\>/, "&gt;").replace(/\n/, "<br>")
}

var addedApplicationSpecificFor = "";
function AddApplicationSpecific(id) {
    if(addedApplicationSpecificFor == id) return;
    addedApplicationSpecificFor = id;
    fetch(`/applicationspecific/${id}`).then(res => {
        if(res.status != 200) {
            document.getElementById("specificContainer").style.display = "none"
            return
        }
        res.text().then(res => {
            document.getElementById("specificContainer").style.display = "block"
            document.getElementById("specific").innerHTML = res
            Array.prototype.forEach.call(document.getElementById("specific").getElementsByTagName("script"), e => {
                s = document.createElement("script")
                s.innerHTML = e.innerHTML
                document.head.appendChild(s)
            })
        })
    })
}

function DownloadVersionPopUp(version, id) {
    GetVersion(version, id).then(v => {
        if(IsHeadsetAndroid(v.parentApplication.hmd)) {
            PopUp(`<div>Do you want to Download version ${version}.</div>
                    <div style="display: flex;">
                        <input type="button" onclick="AndroidDownload('${v.id}', '${v.parentApplication.id}', '${v.parentApplication.displayName.replace("'", "\\'")}', '${v.version}', false)" value="Yes">
                        <input type="button" onclick="document.getElementById('popup').click()" value="No">
                    </div>`)
        } else {
            PopUp(`<div>Do you want to Download version ${version}.</div>
                    <div style="display: flex;">
                        <input type="button" onclick="RiftDownloadPopUp('${v.parentApplication.id}','${v.id}','${v.version}', '${v.parentApplication.displayName.replace("'", "\\'")}')" value="Yes">
                        <input type="button" onclick="document.getElementById('popup').click()" value="No">
                    </div>`)
        }
    }).catch(err => {
        console.log("Ouch")
    })
}

function GetVersion(version, id, showWorking = true) {
    return new Promise((resolve, reject) => {
        try {
            var res = null;
            connected.versions.forEach(v => {
                if(v.version == version && (!v.binary_release_channels || v.binary_release_channels.nodes.length > 0)) res = v
            });
            if(!res) {
                if(showWorking) {

                    PopUp(`
                    <div>Couldn't find that version. Please try again later.</div>
                    <div>
                        <input type="button" value="OK" onmousedown="MouseDown(event)" onmouseup="if(MouseUp(event)) { ClosePopUp(); }">
                    </div>`)
                }
                reject("not found")
                return
            }
            resolve(res)
        } catch{
            if(showWorking) PopUp(`<div>Working... ${loader}</div>`)
            fetch(`/api/v1/versions/${id}`).then(res => res.json().then(versions => {
                var res = null;
                versions.forEach(v => {
                    if(v.version == version && (!v.binary_release_channels || v.binary_release_channels.nodes.length > 0)) res = v
                });
                if(showWorking && !res) {
                    PopUp(`
                    <div>Couldn't find that version. Please try again later.</div>
                    <div>
                        <input type="button" value="OK" onmousedown="MouseDown(event)" onmouseup="if(MouseUp(event)) { ClosePopUp(); }">
                    </div>`)

                    reject("not found")
                    return;
                }
                resolve(res)
            }))
        }
        
    })
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
    return `<div class="application" id="anchor_${htmlid}" oncontextmenu="ContextMenuEnabled(event, this)" cmon-0="Copy link" cmov-0="Copy(GetIdLink('${dlc.id}'))">
    <div class="info">
        <div class="flex outside">
            <div class="buttons">
            ${dlc.latestAssetFileId ? `<input type="button" value="Download" onmousedown="MouseDown(event)" onmouseup="if(MouseUp(event)) DownloadID('${dlc.latestAssetFileId}')">` : ``}
                
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
                <tr><td class="label">Description</td><td class="value">${dlc.display_short_description ? dlc.display_short_description.replace("\n", "<br>") : "no description"}</td></tr>
                <tr><td class="label">Price</td><td class="value">${dlc.current_offer.price.formatted}</td></tr>
                <tr><td class="label">latest asset file id</td><td class="value">${dlc.latestAssetFileId ? dlc.latestAssetFileId : "No asset file"}</td></tr>
                <tr><td class="label">Id</td><td class="value">${dlc.id}</td></tr>
                <tr><td class="label">Scraped by</td><td class="value">${dlc.__sn}</td></tr>
            </table>
        </div>
    </div>
</div>`
}

function FormatDLCPack(dlc, dlcs, htmlid = "") {
    var included = ""
    dlc.bundle_items.forEach(d => {
        var dlc = GetDLC(dlcs, d.id)
        if(!dlc) {
            dlc = {
                id: "unknown",
                display_name: "Unknown DLC",
                latestAssetFileId: "0",
                downloads: 0,
                display_short_description: "This is an DLC that is now recognized by OculusDB at the time",
                current_offer: {
                    price: {
                        formatted: "0€"
                    }
                }
            }
        }
        included += FormatDLC(dlc, htmlid + "_" + d.id)
    })
    return `<div class="application" id="anchor_${dlc.id}" oncontextmenu="ContextMenuEnabled(event, this)" cmon-0="Copy link" cmov-0="Copy(GetIdLink('${dlc.id}'))">
    <div class="info">
        <div class="flex outside">
            <div class="buttons">
                <input type="button" value="Download" onmousedown="MouseDown(event)" onmouseup="if(MouseUp(event)) DownloadIDList('${dlc.bundle_items.map(x => x.id).join(",")}')">
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
                <tr><td class="label">Scraped by</td><td class="value">${dlc.__sn}</td></tr>
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
                <tr><td class="label">Description</td><td class="value">${a.displayShortDescription == null ? "No description available" : a.displayShortDescription.replace("\n", "<br>")}</td></tr>
                <tr><td class="label">Price</td><td class="value">${a.priceFormatted}</td></tr>
                <tr><td class="label">DLC id</td><td class="value">${a.id}</td></tr>
                <tr><td class="label">Parent Application</td><td class="value">${FormatParentApplication(a.parentApplication, htmlid)}</td></tr>
                <tr><td class="label">Activity id</td><td class="value">${a.__id}</td></tr>
                <tr><td class="label">Scraped by</td><td class="value">${a.__sn}</td></tr>
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
            <div class="flex header" onclick="RevealDescription('${i}_${a.id}')" >
                <div style="padding: 15px; font-weight: bold; color: var(--highlightedColor);" id="${i}_${a.id}_trigger" class="anim noselect">&gt;</div>
                <div stlye="font-size: 1.25em;">${a.displayName}</div>
            </div>
        </div>

        <div class="hidden" id="${i}_${a.id}">
            <table>
                <colgroup>
                    <col width="130em">
                    <col width="100%">
                </colgroup>
                <tr><td class="label">Description</td><td class="value">${a.displayShortDescription == null ? "No description available" : a.displayShortDescription.replace("\n", "<br>")}</td></tr>
                <tr><td class="label">DLC id</td><td class="value">${a.id}</td></tr>
            </table>
        </div>
    </div>
</div>`
}

function DownloadIDList(ids) {
    ids.split(",").forEach(x => {
        DownloadID(x)
    })
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
                <input type="button" value="Details" onmousedown="MouseDown(event)" onmouseup="if(MouseUp(event)) OpenActivity('${a.__id}')">
            </div>
            <div class="flex header" onclick="RevealDescription('${htmlid}')">
                <div>${GetTimeString(a.__lastUpdated)}</div>
                <div style="padding: 15px; font-weight: bold; color: var(--highlightedColor);" id="${htmlid}_trigger" class="anim noselect">&gt;</div>
                <div stlye="font-size: 1.25em;">DLC Pack ${a.displayName} ${a.__OculusDBType == "ActivityNewDLCPack" ? " has been added to " : " has been updated for "} <b>${a.parentApplication.displayName}</b></div>
            </div>
        </div>

        <div class="hidden" id="${htmlid}">
            
            <table>
                <colgroup>
                    <col width="200em">
                    <col width="100%">
                </colgroup>
                <tr><td class="label">Description</td><td class="value">${a.displayShortDescription == null ? "No description available" : a.displayShortDescription.replace("\n", "<br>")}</td></tr>
                <tr><td class="label">Price</td><td class="value">${a.priceFormatted}</td></tr>
                <tr><td class="label">Included DLCs</td><td class="value">${included}</td></tr>
                <tr><td class="label">Parent Application</td><td class="value">${FormatParentApplication(a.parentApplication, htmlid)}</td></tr>
                <tr><td class="label">DLC pack id</td><td class="value">${a.id}</td></tr>
                <tr><td class="label">Activity id</td><td class="value">${a.__id}</td></tr>
                <tr><td class="label">Scraped by</td><td class="value">${a.__sn}</td></tr>
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

function IsStarred(id) {
    var starred = localStorage.starred;
    if(!starred) {
        localStorage.starred = "[]";
        return false;
    }
    var starredArray = JSON.parse(starred);
    return starredArray.includes(id);
}

function SetStarred(id, s) {
    var starred = localStorage.starred;
    if(!starred) {
        localStorage.starred = "[]";
        starred = localStorage.starred;
    }
    var starredArray = JSON.parse(starred);
    if(starredArray.includes(id) && !s) {
        starredArray.splice(starredArray.indexOf(id), 1);
    } else if(!starredArray.includes(id) && s) {
        starredArray.push(id);
    }
    localStorage.starred = JSON.stringify(starredArray);
}
        
 
function GetStarForId(id) {
    return IsStarred(id) ? "&#9733;" : "&#9734;";
}

function UpdateStarredForId(id, element, event) {
    event.cancelBubble = true;
    SetStarred(id, !IsStarred(id));
    element.innerHTML = GetStarForId(id);
}

function FormatApplication(application, htmlId = "", expanded = false) {
    return `<div class="application" oncontextmenu="ContextMenuEnabled(event, this)" cmon-0="Copy link" cmov-0="Copy(GetIdLink('${application.id}'))" cmon-1="Copy Oculus link" cmov-1="Copy('${GetOculusLink(application.id, application.hmd)}')">
    <div class="info">
        <div class="flex outside">
            <div class="buttons">
                <input type="button" value="Details" onmousedown="MouseDown(event)" onmouseup="if(MouseUp(event)) OpenApplication('${application.id}')">
                <input type="button" value="View Activity" onmousedown="MouseDown(event)" onmouseup="if(MouseUp(event)) OpenRecentActivity('${application.id}')">
            </div>
            <div class="flex header" style="${application.blocked ? `color: var(--red);` : ``}" onclick="RevealDescription('${htmlId}')">
                <div style="padding: 15px; font-weight: bold; color: var(--highlightedColor);" id="${htmlId}_trigger" class="anim noselect${expanded ? " rotate" : ""}">&gt;</div>
                <a class="star" onclick="UpdateStarredForId('${application.id}', this, event)">${GetStarForId(application.id)}</a>
                <img alt="Icon of ${application.displayName}" onerror="this.src = '/notfound.jpg'" src="${application.imageLink}" style="max-height: 4em; width: auto; margin-right: 10px;">
                <div stlye="font-size: 1.25em;">${application.displayName} (${ GetHeadsetNames(application.supported_hmd_platforms)})</div>
            </div>
            
        </div>
        <div class="${expanded ? ``: `hidden`}" id="${htmlId}">
            <table>
                <colgroup>
                    <col width="200em">
                    <col width="100%">
                </colgroup>
                <tr><td class="label">Description</td><td class="value">${application.display_long_description.replace("\n", "<br>")}</td></tr>
                <tr><td class="label">Discount</td><td class="value">${application.appHasDiscount ? application.current_offer.promo_benefit + "  ends " + new Date(application.discountEndTime).toLocaleString() : "None"}</td></tr>

                <tr><td class="label">Current price</td><td class="value">${application.appCanBeBought ? application.priceFormatted : "Not purchasable"}</td></tr>
                <tr><td class="label">Baseline price</td><td class="value">${application.baseline_offer != null ? application.baseline_offer.price.formatted : "Not available"}</td></tr>
                
                <tr><td class="label">Has trial</td><td class="value">${application.appHasTrial ? `True<br>${application.current_trial_offer.descriptions.join('<br>').replace("\n", "<br>")}` : `False`}</td></tr>
                <tr><td class="label">Rating</td><td class="value">${application.quality_rating_aggregate ? application.quality_rating_aggregate.toFixed(2) : "Not available"}</td></tr>
                <tr><td class="label">Supported Headsets</td><td class="value">${GetHeadsets(application.supported_hmd_platforms)}</td></tr>
                <tr><td class="label">Publisher</td><td class="value">${application.publisher_name}</td></tr>
                <tr><td class="label">Website URL</td><td class="value">${application.website_url ?? "No entry"}</td></tr>
                <tr><td class="label">Genres</td><td class="value">${application.genre_names.join(", ") ?? "No genres"}</td></tr>
                <tr><td class="label">Is AppLab</td><td class="value">${application.is_concept}</td></tr>
                <tr><td class="label">Is Approved</td><td class="value">${application.is_approved}</td></tr>
                <tr><td class="label">Has ads</td><td class="value">${application.has_in_app_ads}</td></tr>
                <tr><td class="label">Release time</td><td class="value">${new Date(application.releaseDate).toLocaleString()}</td></tr>
                <tr><td class="label">Package name</td><td class="value">${application.packageName ? application.packageName : "Not available"}</td></tr>
                <tr><td class="label">Canonical name</td><td class="value">${application.canonicalName}</td></tr>
                <tr><td class="label">Link to Oculus</td><td class="value"><a href="${GetOculusLink(application.id, application.hmd)}">${GetOculusLink(application.id, application.hmd)}</a></td></tr>
                <tr><td class="label">Id</td><td class="value">${application.id}</td></tr>
                <tr><td class="label">Scraped by</td><td class="value">${application.__sn}</td></tr>
            </table>
        </div>
    </div>

</div>`
}

function FormatApplicationUpdatedActivity(a, htmlid) {
    return `<div class="application" oncontextmenu="ContextMenuEnabled(event, this)" cmon-0="Copy activity link" cmov-0="Copy(GetActivityLink('${a.__id}'))">
    <div class="info">
        <div class="flex outside">
            <div class="buttons">
                <input type="button" value="Details" onmousedown="MouseDown(event)" onmouseup="if(MouseUp(event)) OpenActivity('${a.__id}')">
                <input type="button" value="View Application" onmousedown="MouseDown(event)" onmouseup="if(MouseUp(event)) OpenApplication('${a.newApplication.id}')">
            </div>
            <div class="flex header" onclick="RevealDescription('${htmlid}')">
                <div>${GetTimeString(a.__lastUpdated)}</div>
                <div style="padding: 15px; font-weight: bold; color: var(--highlightedColor);" id="${htmlid}_trigger" class="anim noselect">&gt;</div>
                <div stlye="font-size: 1.25em;">Application Updated! <b>${a.newApplication.displayName}</b></div>
            </div>
        </div>

        <div class="hidden" id="${htmlid}">
            <div class="leftRightAdjustedContainer">
                <div class="tabContainer leftRightAdjustedContainerItem">
                    <h2>Old</h2>
                    ${FormatApplication(a.oldApplication, `${htmlid}_${a.__id}_old`, true)}
                </div>
                <div class="leftRightAdjustedContainerItem">
                    <h2>New</h2>
                    ${FormatApplication(a.newApplication, `${htmlid}_${a.__id}_new`, true)}
                </div>
            </div>
        </div>
    </div>
</div>`
}

function FormatJsonToTable(json) {
    var longest = 0;
    var content = ``
    for(const [key, value] of Object.entries(json)) {
        var n = key.replace(/_/g, " ")
        if(n.length > longest) longest = n.length
        content += `<tr>
            <td className="label">${n}</td>
            <td className="value">${value}</td>
        </tr>`
    }
    var table = `<table>
                <colgroup>
                    <col width="${longest * 10 + 20}em">
                    <col width="100%">
                </colgroup>
                ${content}
            </table>
            `
    return table;
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
                <tr><td class="label">Scraped by</td><td class="value">${a.__sn}</td></tr>
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
                <tr><td class="label">Scraped by</td><td class="value">${a.__sn}</td></tr>
            </table>
        </div>
    </div>
    
</div>`
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

function GetObbs(downloadable, obb, v) {
    if(!obb) return "We are working on getting obbs for you. Please check again in a few minutes. This may take longer depending on what's to do. Thanks!"
    if(obb.length == 0) return `none`
    var obbs = ``
    for(const o of obb) {
        obbs +=`<div class="application">
        <div class="info">
            <div class="flex outside">
            <div class="buttons">
                ${GetDownloadButtonVersion(downloadable, o.id, v.parentApplication.hmd, v.parentApplication, v.version, true)}
            </div>
                <div class="flex header" onclick="RevealDescription('${v.id}_${o.id}')">
                    <div style="padding: 15px; font-weight: bold; color: var(--highlightedColor);" id="${v.id}_${o.id}_trigger" class="anim noselect">&gt;</div>
                    <div stlye="font-size: 1.25em;">${o.file_name}</div>
                </div>
            </div>

            <div class="hidden" id="${v.id}_${o.id}">
                <table>
                    <colgroup>
                        <col width="110em">
                        <col width="100%">
                    </colgroup>
                    <tr><td class="label">file name</td><td class="value">${o.file_name}</td></tr>
                    <tr><td class="label">size</td><td class="value">${o.sizeString}</td></tr>
                    <tr><td class="label">Id</td><td class="value">${o.id}</td></tr>
                </table>
            </div>
        </div>
    </div>`
    }
    return obbs
}

function FormatVersion(v, htmlid = "") {
    var releaseChannels = ""
    if(v.binary_release_channels) {
        v.binary_release_channels.nodes.forEach(x => {
            releaseChannels += `${x.channel_name}, `
        })
    }
    if(releaseChannels.length > 0) releaseChannels = releaseChannels.substring(0, releaseChannels.length - 2)
    var downloadable = releaseChannels != "" || !v.binary_release_channels
    return `<div class="application" id="anchor_${v.id}" oncontextmenu="ContextMenuEnabled(event, this)" cmon-0="Copy link" cmov-0="Copy(GetIdLink('${v.id}'))">
    <div class="info">
        <div id="anchor" style="height: 0;"></div>
        <div class="flex outside">
            <div class="buttons">
                ${GetDownloadButtonVersion(downloadable, v.id, v.parentApplication.hmd, v.parentApplication, v.version, false, v.obbList ? v.obbList.map(x => x.id).join(",") : "", v.obbList ? v.obbList.map(x => x.file_name).join("/") : "")}
            </div>
            <div class="flex header" onclick="RevealDescription('${htmlid}')">
                <div style="padding: 15px; font-weight: bold; color: var(--highlightedColor);" id="${htmlid}_trigger" class="anim noselect">&gt;</div>
                <div stlye="font-size: 1.25em;">${v.version} &nbsp;&nbsp;&nbsp;&nbsp;(${(v.alias ? `<b>${v.alias}</b>; ` : "") + v.versionCode + (v.downloads ? `; ${v.downloads}` : ``)})</div>
            </div>
            
        </div>

        <div class="hidden" id="${htmlid}">
            <table>
                <colgroup>
                    <col width="230em">
                    <col width="100%">
                </colgroup>
                ${v.downloads ? `<tr><td class="label">Downloads</td><td class="value">${v.downloads}</td></tr>` : ""}
                <tr><td class="label">Uploaded</td><td class="value">${new Date(v.created_date * 1000).toLocaleString()}</td></tr>
                <tr><td class="label">Release Channels</td><td class="value">${downloadable ? releaseChannels : "none"}</td></tr>
                <tr><td class="label">Downloadable</td><td class="value">${downloadable}</td></tr>
                <tr><td class="label">Version</td><td class="value">${v.version}</td></tr>
                <tr><td class="label">Version code</td><td class="value">${v.versionCode}</td></tr>
                <tr><td class="label">Changelog</td><td class="value">${GetChangelog(v)}</td></tr>
                <tr><td class="label">Obbs</td><td class="value">${GetObbs(downloadable, v.obbList, v)}</td></tr>
                <tr><td class="label">Id</td><td class="value">${v.id}</td></tr>
                <tr><td class="label">Last scraped</td><td class="value">${GetTimeString(v.lastScrape)}</td></tr>
                <tr><td class="label">Last priority scraped</td><td class="value">${GetTimeString(v.lastPriorityScrape)}</td></tr>
                <tr><td class="label">Scraped by</td><td class="value">${v.__sn}</td></tr>
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
                <tr><td class="label">Scraped by</td><td class="value">${v.__sn}</td></tr>
            </table>
        </div>
    </div>
</div>`
}

function FormatChangelogActivity(v, htmlid) {
    var releaseChannels = ""
    v.releaseChannels.forEach(x => {
        releaseChannels += `${x.channel_name}, `
    })
    if(releaseChannels.length > 0) releaseChannels = releaseChannels.substring(0, releaseChannels.length - 2)
    return `<div class="application" oncontextmenu="ContextMenuEnabled(event, this)" cmon-0="Copy activity link" cmov-0="Copy(GetActivityLink('${v.__id}'))">
    <div class="info">
        <div class="flex outside">
            <div class="buttons">
                <input type="button" value="Details" onmousedown="MouseDown(event)" onmouseup="if(MouseUp(event)) OpenActivity('${v.__id}')">
                ${GetDownloadButtonVersion(v.downloadable, v.id, v.parentApplication.hmd, v.parentApplication, v.version)}
            </div>
            <div class="flex header" onclick="RevealDescription('${htmlid}')">
                <div>${GetTimeString(v.__lastUpdated)}</div>
                <div style="padding: 15px; font-weight: bold; color: var(--highlightedColor);" id="${htmlid}_trigger" class="anim noselect">&gt;</div>
                <div stlye="font-size: 1.25em;">Version <b>${v.version} &nbsp;&nbsp;&nbsp;&nbsp;(${v.versionCode})</b>s Changelog of <b>${v.parentApplication.displayName}</b> ${v.__OculusDBType == "ActivityVersionChangelogUpdated" ? `has been updated` : `is now available`}</div>
            </div>
        </div>
        
        <div class="hidden" id="${htmlid}">
            <table>
                <colgroup>
                    <col width="200em">
                    <col width="100%">
                </colgroup>
                <tr><td class="label">Uploaded</td><td class="value">${new Date(v.uploadedTime).toLocaleString()}</td></tr>
                <tr><td class="label">Release Channels</td><td class="value">${v.downloadable ? releaseChannels : "none"}</td></tr>
                <tr><td class="label">Downloadable</td><td class="value">${v.downloadable}</td></tr>
                <tr><td class="label">Version</td><td class="value">${v.version}</td></tr>
                <tr><td class="label">Version code</td><td class="value">${v.versionCode}</td></tr>
                <tr><td class="label">Changelog</td><td class="value">${FormatChangelog(v.changeLog)}</td></tr>
                <tr><td class="label">Parent Application</td><td class="value">${FormatParentApplication(v.parentApplication, htmlid)}</td></tr>
                <tr><td class="label">Id</td><td class="value">${v.id}</td></tr>
                <tr><td class="label">Activity id</td><td class="value">${v.__id}</td></tr>
                <tr><td class="label">Scraped by</td><td class="value">${v.__sn}</td></tr>
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
    if(e.__OculusDBType == "ActivityApplicationUpdated") return FormatApplicationUpdatedActivity(e, htmlid)
    if(e.__OculusDBType == "ActivityPriceChanged") return FormatPriceChanged(e, htmlid)
    if(e.__OculusDBType == "ActivityVersionChangelogAvailable" || e.__OculusDBType == "ActivityVersionChangelogUpdated") return FormatChangelogActivity(e, htmlid)
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

var data = {}

function OpenDownloadWithJokes(id, openObb) {
    if(jokeconfig.dialupdownload) {
        PlaySound("/cdn/modem.ogg")
        TextBoxText("downloadTextBox", "Dialing Oculus")
        setTimeout(() => {
            TextBoxText("downloadTextBox", "Asking for entitlement")
            setTimeout(() => {
                TextBoxText("downloadTextBox", "Entitlement check cannot be made")
                setTimeout(() => {
                    TextBoxText("downloadTextBox", "Can you try again please?")
                    setTimeout(() => {
                        TextBoxText("downloadTextBox", "sure")
                        setTimeout(() => {
                            TextBoxText("downloadTextBox", "Trying again...")
                            setTimeout(() => {
                                TextBoxText("downloadTextBox", "Nope still nothing...")
                                setTimeout(() => {
                                    TextBoxText("downloadTextBox", "ok, I give up")
                                    setTimeout(() => {
                                        RealDownload(id, openObb)
                                    }, 1300);
                                }, 4200);
                            }, 7000);
                        }, 1200);
                    }, 800);
                }, 1000);
            }, 1000);
        }, 7000);
    } else RealDownload(id, openObb)
}

function RealDownload(id, openObb) {
    window.open(GetDownloadLink(id));
    ClosePopUp();
    if(openObb) ObbDownloadPopUp();
}

function AndroidDownload(id, parentApplicationId,parentApplicationName, version, isObb = false, obbIds = "", obbNames = "") {
    var obbs = []
    if(obbIds) {
        var obbIdsSplit = obbIds.split(",")
        var obbNamesSplit = obbNames.split("/")
        for(let i = 0; i < obbIds.length; i++) {
            obbs.push({
                id: obbIdsSplit[i],
                name: obbNamesSplit[i]
            })
        }
    }
    data = {
        type: "Download",
        binaryId: id,
        parentId: parentApplicationId,
        parentName: parentApplicationName,
        version: version,
        isObb: isObb,
        obbList: obbs,
        downloadLink: GetDownloadLink(id)
    }
    if(sendToParent) {
        fetch(`/api/v1/id/${parentApplicationId}`).then(res => res.json().then(res => {
            data.packageName = res.packageName
            SendDataToParent(JSON.stringify(data))
        }))
        return
    }

    // Not in iframe which supports downloads
    if(localStorage.fuckpopups && !jokeconfig.dialupdownload) {
        DownloadID(id)
        if(obbs && obbs.length > 0){
            ObbDownloadPopUp()
        }
        if(isObb && !sendToParent) ObbInfoPopup()
    } else {
        PopUp(`
        <div>
            To download games you must be logged in on <a href="{oculusloginlink}">{oculusloginlink}</a>. If you aren't logged in you won't be able to download games.
            <br>
            <a onclick="localStorage.fuckpopups = 'yummy, spaghetti'; window.open(GetDownloadLink('${id}')); ClosePopUp();"><i style="cursor: pointer;">Don't show warning again</i></a>
            <div class="textbox" id="downloadTextBox"></div>
            <div>
                <input type="button" value="Log in" onmousedown="MouseDown(event)" onmouseup="if(MouseUp(event)) window.open('{oculusloginlink}', )">
                <input type="button" value="Download" onmousedown="MouseDown(event)" onmouseup="if(MouseUp(event)) { OpenDownloadWithJokes('${id}', ${obbs && obbs.length > 0}); ${isObb && !sendToParent ? `ObbInfoPopup();` : ``}}">
            </div>
        </div>
    `)
    }
   
}

function ObbInfoPopup() {
    fetch(`/api/v1/id/${data.parentId}`).then(res => res.json().then(res => {
        data.packageName = res.packageName
        PopUp(`
            <div>
                For the obb to work you have to copy it to the following directory on your quest. Use SideQuest or another file manager: <code>/Android/obb/${data.packageName}/</code>
                <br>
                Need help? Join <a href="{OculusDBDC}">The OculusDB Discord server</a>
                <br>
                <div>
                    <input type="button" value="OK" onmousedown="MouseDown(event)" onmouseup="if(MouseUp(event)) { ClosePopUp(); }">
                </div>
            </div>
        `)
    }))
}

function DownloadObbs(ids, parentApplicationId, parentApplicationName, version, isObb = false, obbId = "") {
    for(const id of ids.split(",")) {
        DownloadIDList(id)
    }
    ObbInfoPopup()
}

function ObbDownloadPopUp() {
    fetch(`/api/v1/id/${data.parentId}`).then(res => res.json().then(res => {
        PopUp(`
                <div>
                    This game requires obb files (extra files that are required for the game to work). Do you want to download them?
                    <div>
                        <input type="button" value="Yes" onmousedown="MouseDown(event)" onmouseup="if(MouseUp(event)) { DownloadObbs('${data.obbList.map(x => x.id).join(",")}', '${data.parentId}', '${data.parentName.replace("'", "\\'")}', '${data.version}', true, null); ClosePopUp(); }">
                        <input type="button" value="No" onmousedown="MouseDown(event)" onmouseup="if(MouseUp(event)) { ClosePopUp(); }">
                    </div>
                </div>
            `)
    }))
}

function GetDownloadButtonVersion(downloadable, id, hmd, parentApplication, version, isObb = false, obbIds = "", obbNames = "") {
    if(IsHeadsetAndroid(hmd)) {
        if(localStorage.isOculusDowngrader) {
            return `<input type="button" value="Download${downloadable ? '"' : ' (Developer only)" class="red"'} onmousedown="MouseDown(event)" onmouseup="if(MouseUp(event)) AndroidDownloadPopUp('${parentApplication.id}','${id}', '${hmd}')" oncontextmenu="ContextMenuEnabled(event, this)">`
        }
        return `<input type="button" value="Download${downloadable ? '"' : ' (Developer only)" class="red"'} onmousedown="MouseDown(event)" onmouseup="if(MouseUp(event)) AndroidDownload('${id}', '${parentApplication.id}', '${parentApplication.displayName.replace("'", "\\'")}', '${version}', ${isObb}, ${obbIds == null ? "null" : `'${obbIds}'`}, ${obbNames == null ? "null" : `'${obbNames}'`})" oncontextmenu="ContextMenuEnabled(event, this)" cmon-0="Copy download url" cmov-0="Copy(GetDownloadLink('${id}'))" cmon-1="Show Oculus Downgrader code" cmov-1="AndroidDownloadPopUp('${parentApplication.id}','${id}', '${hmd}')">`
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
    document.cookie = name + "=" + value + ";" + expires + ";";
}

const dict = {
    "search": "Sniff :3",
    "oculusdb": "UwUculusDB",
    "oculusdb.": "UwUculusDB.",
    "application": "Home",
    "applications": "Homes",
    "yes": "yes daddy",
    "no": "no D:",
    "beat": "UwU",
    "saber": "senpai",
    "query": "Sniff term",
    "versions": "wowzees",
    "details": "the tails",
    "show": "showo",
    "downloads)": "downloaded by furries)",
    "downloads": "downloaded by furries",
    "live": "READY TO BE EGGSPLORED",
    "download": "steal `w´"
}

function OwOify(text) {
    if(!text) return ""
    var words = text.split(" ")
    for(i = 0; i < words.length; i++) {
        var lower = words[i].toLowerCase()
        if(dict[lower]) words[i] = dict[lower]
        else {
            //continue;
            words[i] = words[i].replace(/(?:r|l)/g, "w");
            words[i] = words[i].replace(/(?:R|L)/g, "W");
            words[i] = words[i].replace(/n([aeiou])/g, 'ny$1');
            words[i] = words[i].replace(/N([aeiou])/g, 'Ny$1');
            words[i] = words[i].replace(/N([AEIOU])/g, 'Ny$1');
            words[i] = words[i].replace(/ove/g, "uv");
            words[i] = words[i].replace(/th/g, "d");
            words[i] = words[i].replace(/Th/g, "D");
            words[i] = words[i].replace(/TH/g, "D");
            words[i] = words[i].replace(/!+/g, " " + GetRandomFace() + " ");
        }
    }
    return words.join(" ");
}
var now = new Date();
if(now.getMonth() == 3 && now.getDate() == 1) {
    OwO()
    setInterval(OwO, 100)
}
function OwO() {

    var allTags = document.querySelectorAll('*:not(:has(:not(br):not(b):not(i)))');
    
    for (var i = 0, max = allTags.length; i < max; i++) {
        if(allTags[i].changed) continue;
        allTags[i].changed = true
        allTags[i].innerText = OwOify(allTags[i].innerText)
        if(allTags[i].value && allTags[i].tagName == "INPUT" && allTags[i].type == "button") {
            allTags[i].value = OwOify(allTags[i].value)
        }
        if(allTags[i].placeholder) {
            allTags[i].placeholder = OwOify(allTags[i].placeholder)
        }
    }
}

function GetRandomFace() {
    var faces = ["(・`ω´・)", ";;w;;", "owo", "UwU", ">w<", "^w^", "(*^ω^)", "(◕‿◕✿)", "(◕ᴥ◕)", "ʕ•ᴥ•ʔ"];
    return faces[Math.floor(Math.random() * faces.length)];
}