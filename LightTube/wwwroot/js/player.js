try {
	document.querySelector(".player-container").style.marginBottom = "0";
} catch (_) {
	
}

const oldVol = localStorage.getItem("ltvideo.volume");
if (oldVol) {
	localStorage.setItem("ltplayer.volume", oldVol);
	localStorage.removeItem("ltvideo.volume");
}

const player = new Player(`video#${playerId}`, videoInfo, playtype);