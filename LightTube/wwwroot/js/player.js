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

document.querySelectorAll("a[href*='?v="+videoId+"&t=']").forEach(x => {
	const t = Number(x.getAttribute("href").split("t=")[1].replace(/[^0-9]/, ""));
	x.onclick =  e => {
		e.preventDefault();
		player.player.currentTime = t;
		player.player.scrollIntoView({behavior: "smooth"});
	};
});