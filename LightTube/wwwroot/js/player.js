const opts = {
	liveui: true,
	playbackRates: [
		0.25, 0.50, 0.75, 1.00,
		1.25, 1.50, 1.75, 2.00
	],
	preloadTextTracks: false,
	plugins: {
		hotkeys: {
			volumeStep: 0.1,
			seekStep: 5,
			enableModifiersForNumbers: false,
			enableVolumeScroll: false,
		},
		endscreen: endscreenData,
		vttThumbnails: {
			src: `/proxy/storyboard/${videoId}.vtt`,
			showTimestamp: true
		}
	},
	sources: []
};

switch (playtype) {
	case "dash":
		opts.sources.push({
			src: `/proxy/media/${videoId}.mpd`,
			type: "application/dash+xml"
		});
		break;
	case "hls":
		opts.sources.push({
			src: `/proxy/media/${videoId}.m3u8`,
			type: "application/x-mpegURL"
		});
		break;
	case "html5":
		opts.sources = undefined;
		break;
}

const player = videojs(elementId, opts, function () {
	const v = localStorage.getItem("ltvideo.volume");
	if (v != null)
		this.volume(localStorage.getItem("ltvideo.volume"))
	this.controlBar.addChild('QualitySelector', {}, 14);
	this.qualityLevels();
	this.hlsQualitySelector();
	
	this.on("volumechange", _ => {
		localStorage.setItem("ltvideo.volume", this.volume())
	})
});