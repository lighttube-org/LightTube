videoInfo.buttons = {
	play: '<svg><use href="/svg/bootstrap-icons.svg#play-fill"/></svg>',
	pause: '<svg><use href="/svg/bootstrap-icons.svg#pause-fill"/></svg>',
	volumeMute: '<svg><use href="/svg/bootstrap-icons.svg#volume-mute-fill"/></svg>',
	volumeLow: '<svg><use href="/svg/bootstrap-icons.svg#volume-off-fill"/></svg>',
	volumeMedium: '<svg><use href="/svg/bootstrap-icons.svg#volume-down-fill"/></svg>',
	volumeHigh: '<svg><use href="/svg/bootstrap-icons.svg#volume-up-fill"/></svg>',
	settings: '<svg><use href="/svg/bootstrap-icons.svg#gear-fill"/></svg>',
	fullscreen: '<svg><use href="/svg/bootstrap-icons.svg#fullscreen"/></svg>',
	minimize: '<svg><use href="/svg/bootstrap-icons.svg#fullscreen-exit"/></svg>'
};
const player = new Player("video", videoInfo);