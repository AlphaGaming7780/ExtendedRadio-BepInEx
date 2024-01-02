var ExtendedRadio_radioPanel = document.getElementsByClassName("radio-panel_rXp")[0]

var ExtendedRadio_radioPannelTop = document.getElementsByClassName("title-bar_PF4")[0]
var ExtendedRadio_space = document.getElementsByClassName("icon-space_h_f")[0]

var ExtendedRadio_radioPanelBottom = document.getElementsByClassName("content_r9x")[0]
var ExtendedRadio_stationsMenu = document.getElementsByClassName("stations-menu_kAr")[0]
var ExtendedRadio_networks = document.getElementsByClassName("networks_SQ5")[0]
var ExtendedRadio_stations = document.getElementsByClassName("stations_mU1")[0]
var ExtendedRadio_programs = document.getElementsByClassName("list_Kl3")[0]

function ExtendedRadio_getterValue(event, element, onUpdate) {
    const updateEvent = event + "getvalue" + ".update"
    const subscribeEvent = event + "getvalue" + ".subscribe"
    const unsubscribeEvent = event + "getvalue" + ".unsubscribe"
    
    var sub = engine.on(updateEvent, (data) => {
        element && onUpdate(element, data)
    })

    engine.trigger(subscribeEvent)
    return () => {
        engine.trigger(unsubscribeEvent)
        sub.clear()
    };
}
