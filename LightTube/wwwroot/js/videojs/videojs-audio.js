function audioPlugin(options) {
	const audioElement = document.createElement("audio");
	document.body.appendChild(audioElement);
	audioElement.src = options.audioTracks[0].src;

	let seeking = false;
	let startedPlayback = false;
	let paused = false;
	let buffering = false;
	let skipOneUpdate = false;

	this.on("playing", () => {
		startedPlayback = true;
		paused = false;
		audioElement.play();
	});

	this.on("pause", () => {
		paused = true;
		skipOneUpdate = true;
		audioElement.pause();
	});

	audioElement.onplaying = () => {
		startedPlayback = true;
		paused = false;
	};

	this.on("waiting", () => {
		buffering = "video";
	});

	audioElement.onwaiting = () => {
		buffering = "audio";
	};

	this.on("canplaythrough", () => {
		buffering = "";
	});

	audioElement.oncanplaythrough = () => {
		buffering = "";
	};

	this.on("seeking", () => {
		seeking = true;
	});

	this.on("seeked", () => {
		seeking = false;
	});

	this.on("volumechange", () => {
		audioElement.muted = this.muted();
		audioElement.volume = this.volume();
	});

	const update = () => {
		if (startedPlayback) {
			if (Math.abs(this.currentTime() - audioElement.currentTime) > 0.5 && !seeking) {
				audioElement.currentTime = this.currentTime();
			}

			if (!buffering) {
				if (paused && !this.paused()) {
					this.pause();
				}
				if (paused && !audioElement.paused) {
					audioElement.pause();
				}
				if (!paused && this.paused()) {
					this.play();
				}
				if (!paused && audioElement.paused) {
					audioElement.play();
				}
				if (this.paused() !== audioElement.paused) {
					if (this.paused())
						audioElement.pause();
					else
						audioElement.play();
				}
			}
		}

		switch (buffering) {
			case "video":
				audioElement.pause();
				break;
			case "audio":
				this.pause();
				break;
		}
	};
	setInterval(update, 250)
}

videojs.registerPlugin("audio", audioPlugin);
