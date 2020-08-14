export class GameElem extends React.Component {
	constructor(props) {
		super(props);
		this.state = {
			IsFavorite: this.props.IsFavorite === true,
			MobileClick: false
		};
		this.HandleFavourite = this.HandleFavourite.bind(this);
		this.OpenPopup = this.OpenPopup.bind(this);
		this.ClosePopup = this.ClosePopup.bind(this);
	}
	HandleFavourite = () => {
		this.setState({
			IsFavorite: !this.state.IsFavorite
		});

		this.props.HandleFavourite();
	};
	PlayNow = () => {
		if (isLogin === "1") {
			let tpId = this.props.ProviderId;
			let gameId = this.props.GameId;
			window
				.open(
					`${entPageUrl}&fun=${0}&gameId=${gameId}&tpId=${tpId}&gameType=slots`,
					`Tp_${tpId}`,
					"toolbar=no, menubar=no,scrollbars=no,resizable=no,location=no,directories=no,status=no, top=0, left=0"
				)
				.focus();
		} else {
			if (window.ShowLogin) {
				ShowLogin();
			} else {
				var loginPopup = $("[data-remodal-id=modal-login]").remodal();
				if (loginPopup) {
					loginPopup.open();
				}
			}
		}
	};
	PlayFree = () => {
		if (isLogin === "1") {
			let tpId = this.props.ProviderId;
			let gameId = this.props.GameId;
			window
				.open(
					`${entPageUrl}&fun=${1}&gameId=${gameId}&tpId=${tpId}&gameType=slots`,
					`Tp_${tpId}`,
					"toolbar=no, menubar=no,scrollbars=no,resizable=no,location=no,directories=no,status=no, top=0, left=0"
				)
				.focus();
		} else {
			if (window.ShowLogin) {
				ShowLogin();
			} else {
				var loginPopup = $("[data-remodal-id=modal-login]").remodal();
				if (loginPopup) {
					loginPopup.open();
				}
			}
		}
	};
	OpenPopup = e => {
		e.preventDefault();
		e.stopPropagation();
		if (this.props.IsMobileAndTablet) {
			this.setState({
				MobileClick: true
			});
		}
	};
	ClosePopup = e => {
		e.preventDefault();
		e.stopPropagation();
		if (this.props.IsMobileAndTablet) {
			this.setState({
				MobileClick: false
			});
		}
	};
	render() {
		let playFree =
			this.props.ProviderProps.PlayFree === false
				? false
				: this.props.Isfun !== 0;
		let elem = (
			<>
				<div>
					<h3>
						<span>{this.props.ProviderName}</span>
					</h3>
					<span>{this.props.GameName}</span>
					{this.state.IsFavorite === true ? <span>★</span> : null}
				</div>
				{this.state.MobileClick ? (
					<div className="mobilepop" style={{ display: "block" }}>
						<a className="close" onClick={this.ClosePopup}>
							<span>X</span>
						</a>
						<img
							src={`/common/images/Casino/${this.props.ProviderName}/${this.props.ImgUrl}?v=202004073`}
						/>
						<div>
							<span>{this.props.GameName}</span>
							<span>
								{this.state.IsFavorite === true ? "★" : "☆"}
							</span>
						</div>
						<a onClick={this.PlayNow}>
							<span>{i18n.lbl_PlayNow}</span>
						</a>
						{playFree ? (
							<a onClick={this.PlayFree}>
								<span>{i18n.lbl_Free}</span>
							</a>
						) : null}
						<a onClick={this.HandleFavourite}>
							<span>
								{this.state.IsFavorite === true ? "★" : "☆"}
							</span>
						</a>
					</div>
				) : null}
				<div className="playNow" style={{ display: "none" }}>
					<a onClick={this.PlayNow}>
						<span>{i18n.lbl_PlayNow}</span>
					</a>
					{playFree ? (
						<a onClick={this.PlayFree}>
							<span>{i18n.lbl_Free}</span>
						</a>
					) : null}
					<a onClick={this.HandleFavourite}>
						<span>
							{this.state.IsFavorite === true ? "★" : "☆"}
						</span>
					</a>
				</div>
			</>
		);

		return this.props.BigItem ? (
			<div
				style={{
					background: `url(/common/images/Casino/${this.props.ProviderName}/${this.props.ImgUrl}?v=202004073)no-repeat top center / cover`
				}}
				className="BIG"
				onClick={this.OpenPopup}>
				{elem}
			</div>
		) : (
			<div
				style={{
					background: `url(/common/images/Casino/${this.props.ProviderName}/${this.props.ImgUrl}?v=202004073)no-repeat top center / cover`
				}}
				onClick={this.OpenPopup}>
				{elem}
			</div>
		);
	}
}
