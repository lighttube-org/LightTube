const Plugin = videojs.getPlugin('plugin');
const Component = videojs.getComponent('Component');

class EndscreenPlugin extends Plugin {
	player;
	items;
	startMs;
	el;

	constructor(player, options) {
		super(player, options);
		this.player = player;
		this.items = options.items;
		this.startMs = options.startMs;

		this.endscreen = player.addChild("Endscreen", options);
		this.el = this.endscreen.el_;
		this.endscreen.setClickHandler(options.clickHandler);

		this.on(player, ["timeupdate", "seeked"], this.updateEndscreen)
	}

	updateEndscreen() {
		if (this.player.currentTime() * 1000 >= this.startMs) {
			this.el.style.display = "block";
			this.endscreen.updateTime(this.player.currentTime() * 1000);
		} else {
			this.el.style.display = "none";
		}
	}
}

class EndscreenComponent extends Component {
	items = []
	
	constructor(player, options = {}) {
		super(player, options);

		if (options.startMs) {
			this.updateEndscreenItems(options.items);
		}
	}

	// The `createEl` function of a component creates its DOM element.
	createEl() {
		const el = videojs.dom.createEl('div', {
			className: 'vjs-endscreen'
		});
		return el;
	}

	setClickHandler(clickHandler) {
		this.clickHandler = clickHandler;
	}

	updateEndscreenItems(items) {
		const container = this.el();

		for (let item of items) {
			const el = videojs.dom.createEl("div", {
				className: 'vjs-endscreen-item'
			});
			const img = videojs.dom.createEl("img");
			const div = videojs.dom.createEl("div");
			const title = videojs.dom.createEl("span");
			const metadata = videojs.dom.createEl("span");

			el.style.aspectRatio = item.aspectRatio;
			el.style.left = (item.left * 100) + "%";
			el.style.top = (item.top * 100) + "%";
			el.style.width = (item.width * 100) + "%";
			el.setAttribute("data-startms", item.startMs)
			el.setAttribute("data-endms", item.endMs)
			el.addEventListener("click", _ => this.clickHandler(item))

			title.innerText = item.title;
			metadata.innerText = item.metadata;
			img.src = item.image[item.image.length - 1].url;

			div.appendChild(title);
			div.appendChild(metadata);
			el.appendChild(img);
			el.appendChild(div);
			this.items.push(el);
			container.appendChild(el);
		}
	}
	
	updateTime(timeMs) {
		for (let item of this.items) {
			let start = Number(item.getAttribute("data-startms"));
			let end = Number(item.getAttribute("data-endms"));
			if (start < timeMs && timeMs < end) {
				item.style.display = "grid";
			} else {
				item.style.display = "none";
			}
		}
	}
}

videojs.registerComponent('Endscreen', EndscreenComponent);
videojs.registerPlugin('endscreen', EndscreenPlugin);