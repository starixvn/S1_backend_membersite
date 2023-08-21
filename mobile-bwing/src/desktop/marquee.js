(function ($, React, ReactDOM, i18n) {
	class Panel extends React.Component {
		constructor(props) {
			super(props);
			this.state = { datas: [], hover: false, pos: null, initWidth: 0 };
			this.ClickMarquee = this.ClickMarquee.bind(this);
			this.GetAnnouncement = this.GetAnnouncement.bind(this);
		}
		componentDidMount() {
			this.GetAnnouncement();
			//this.intervalId = setInterval(this.GetAnnouncement, 60000);
		}
		// SetPos = () => {
		// 	if (this.state.prev !== null) {
		// 		let outerBox = $(this.refs.content).width();
		// 		this.setState(prev => {
		// 			if (prev.pos >= outerBox) {
		// 				return { pos: -prev.initWidth };
		// 			} else {
		// 				return { pos: prev.pos + 1.5 };
		// 			}
		// 		});
		// 	}
		// };
		componentDidUpdate = () => {
			// if (this.state.pos === null) {
			// 	let pos = $(this.refs.marquee).width();
			// 	this.setState({ pos: -pos, initWidth: pos });
			// }
		};
		GetAnnouncement() {
			axios
				.post(`/api/Message/GetAnnouncement/${SiteLanguage}`, {})
				.then(res => {
					let resData = res.data.data;
					let stateData = this.state.datas;
					let upd = false;
					if (resData.length !== 0 && stateData.length !== 0) {
						for (let d in resData) {
							let find = stateData.find(
								x =>
									x.id === resData[d].id &&
									x.subject === resData[d].subject &&
									x.content === resData[d].content
							);

							if (!find) {
								upd = true;
								break;
							}
						}
					} else {
						upd = true;
					}

					if (upd) {
						// if (this.intervalId_Pos === undefined) {
						// 	this.intervalId_Pos = setInterval(this.SetPos, 25);
						// }

						this.setState({
							datas: res.data.data.map(item => {
								return {
									...item,
									subject: item.subject,
									content: item.content
								};
							})
						});
					}
				});
		}
		ClickMarquee(item) {
			if (announcementUrl) {
				window.location.href = announcementUrl;
			}
		}
		// MarqueeOnMouseOver = () => {
		// 	clearInterval(this.intervalId_Pos);
		// };
		// MarqueeOnMouseOut = () => {
		// 	clearInterval(this.intervalId_Pos);
		// 	this.intervalId_Pos = setInterval(this.SetPos, 25);
		// };
		render() {
			// let rows = this.state.datas.map(item => {
			// 	return (
			// 		<button
			// 			onMouseOut={this.MarqueeOnMouseOut}
			// 			onMouseOver={this.MarqueeOnMouseOver}
			// 			key={`announcement-${item.id}`}
			// 			style={{ paddingLeft: "2rem" }}
			// 			onClick={this.ClickMarquee.bind(this, item)}>
			// 			{item.subject}
			// 		</button>
			// 	);
			// });

			// let style =
			// 	this.state.pos === null ? {} : { right: this.state.pos };

			let data = "";
			if (this.state.datas) {
				this.state.datas.forEach(item => {
					data += item.subject + "　　";
				});
			}

			return (
				<>
					<span className="icon icon-noti"></span>
					<marquee>{data}</marquee>
				</>
			);
		}
	}

	ReactDOM.render(<Panel />, document.getElementById("Marquee"));
})(jQuery, React, ReactDOM, i18n);
