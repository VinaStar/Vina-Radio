/*
  _____           _ _       
 |  __ \         | (_)      
 | |__) |__ _  __| |_  ___  
 |  _  // _` |/ _` | |/ _ \ 
 | | \ \ (_| | (_| | | (_) |
 |_|  \_\__,_|\__,_|_|\___/ 
                            
*/

(function() {
	
	// Listen to game
	window.addEventListener("message", function(event) {
		
		var action = event.data.action;
		var data = event.data.data;
		
		console.log("Vina Radio received message from game: ", action, data);
			
		switch(action) {
			case "ShowRadioSwitcher":
				ShowRadioSwitcher();
				break;
			
			case "HideRadioSwitcher":
				HideRadioSwitcher();
				break;
				
			case "AddRadioChannel":
				AddRadioChannel(data.Channel, data.Label);
				break;
				
			case "SelectRadioChannelIndex":
				SelectRadioChannelIndex(data);
				break;
		}
		
	});
	
	function getContainer() {
		return $("ui").find("#container");
	}
	
	function createRadioChannel(name, label) {
		getContainer().append($('<div class="channel-icon"><div class="img-wrap"><img src="images/' + name + '.png" /></div><span>' + label + '</span></div>'));
	}
	
	var allChannels = [
		"OFF",
		"RADIO_01_CLASS_ROCK",
		"RADIO_02_POP",
		"RADIO_03_HIPHOP_NEW",
		"RADIO_04_PUNK",
		"RADIO_05_TALK_01",
		"RADIO_06_COUNTRY",
		"RADIO_07_DANCE_01",
		"RADIO_08_MEXICAN",
		"RADIO_09_HIPHOP_OLD",
		"RADIO_11_TALK_02",
		"RADIO_12_REGGAE",
		"RADIO_13_JAZZ",
		"RADIO_14_DANCE_02",
		"RADIO_15_MOTOWN",
		"RADIO_16_SILVERLAKE",
		"RADIO_17_FUNK",
		"RADIO_18_90S_ROCK",
		"RADIO_20_THELAB",
		"RADIO_21_DLC_XM17",
		"RADIO_22_DLC_BATTLE_MIX1_RADIO",
	];
	
	/*
	    _   ___ _____ ___ ___  _  _ ___ 
	   /_\ / __|_   _|_ _/ _ \| \| / __|
	  / _ \ (__  | |  | | (_) | .` \__ \
	 /_/ \_\___| |_| |___\___/|_|\_|___/
	 
	*/
	
	function ShowRadioSwitcher() {
		getContainer().addClass("visible");
	}
	
	function HideRadioSwitcher() {
		getContainer().removeClass("visible");
	}
	
	function SelectRadioChannelIndex(channelIndex) {
		var channelIcons = getContainer().find(".channel-icon");
		$(channelIcons).removeClass("pre-selected").removeClass("selected").removeClass("post-selected");
		$(channelIcons[channelIndex - 3]).addClass("pre-selected");
		$(channelIcons[channelIndex - 2]).addClass("pre-selected");
		$(channelIcons[channelIndex - 1]).addClass("pre-selected");
		$(channelIcons[channelIndex]).addClass("selected");
		$(channelIcons[channelIndex + 1]).addClass("post-selected");
		$(channelIcons[channelIndex + 2]).addClass("post-selected");
		$(channelIcons[channelIndex + 3]).addClass("post-selected");
	}
	
	function AddRadioChannel(channelIndex, channelLabel) {
		createRadioChannel(allChannels[channelIndex], channelLabel);
	}
	
	function ToggleRadioChannel(channelIndex, isHidden) {
		
	}
	
})();