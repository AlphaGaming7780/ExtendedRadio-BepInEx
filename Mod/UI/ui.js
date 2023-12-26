// Get the element with the class 'networks_SQ5'
// var element = document.querySelector('.networks_SQ5');

var networks = document.getElementsByClassName("networks_SQ5")[0]
var radioPanel = document.getElementsByClassName("stations-menu_kAr")[0]
var stations = document.getElementsByClassName("scrollable_DXr")[0]

var networkPanel = document.createElement("div")
networkPanel.className = "network_container"
networkPanel.style.overflow = "auto"
networkPanel.style.maxHeight = "100%"
networkPanel.style.paddingTop = "0"
networkPanel.style.paddingLeft = "0"
networkPanel.style.paddingRight = "0"
networkPanel.style.paddingBottom = "0"
networkPanel.style.position = "relative"

networkPanel.appendChild(networks)
radioPanel.insertBefore(networkPanel, stations)
// radioPanel.appendChild(networkPanel)