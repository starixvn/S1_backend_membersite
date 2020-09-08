import React, { Component } from "react";
import PopupOTP from "./popup.otp";
import WebAPI from "../../service/api/base";

export default class Panel extends Component {
	constructor(props) {
		super(props);
		this.state = {
			loading: true,
			errMsg: "",
			request_id: "",
			code: "",
			errCode: 0
		};
	}

	componentDidMount() {
		this.SendVerifySMS();
	}

	SendVerifySMS = () => {
		this.setState({ loading: true });
		WebAPI()
			.post("/api/AccountStrict/SendVerifySMS")
			.then(
				res => {
					let errMsg = "";
					let request_id = "";

					if (res.data.errorCode === 0) {
						request_id = res.data.message;
					} else {
						errMsg = res.data.message;
					}

					this.setState({
						loading: false,
						request_id: request_id,
						errMsg: errMsg,
						errCode: res.data.errorCode
					});
				},
				err => {
					console.log(`ErrorCode:999`);
					console.log(err);
					this.setState({
						errCode: 999,
						loading: false,
						errMsg: "API Server Error."
					});
				}
			);
	};

	ClickVerify = async () => {
		if (!this.state.code) {
			this.setState({ errMsg: i18n.lbl_PleaseEnterCode });
		} else {
			this.setState({ loading: true });
			await WebAPI()
				.post("/api/AccountStrict/VerifyPhone", {
					request_id: this.state.request_id,
					code: this.state.code
				})
				.then(
					res => {
						let errMsg = "";

						if (res.data.errorCode === 0) {
							location.reload();
							// this.ClickClose();
						} else {
							errMsg = res.data.message;
						}

						this.setState({
							errMsg: errMsg,
							loading: false
						});
					},
					err => {
						this.setState({
							loading: false,
							errMsg: "API Server Error."
						});
					}
				);
		}
	};

	ClickClose = () => {
		this.props.HandleShowOTP(false);
	};

	ClickResend = () => {
		this.SendVerifySMS();
	};

	OnChangeCode = e => {
		let value = e.target.value;
		this.setState({ code: value });
	};

	render() {
		return (
			<PopupOTP
				isOpen={this.props.isOpen}
				ClickClose={this.ClickClose}
				ClickVerify={this.ClickVerify}
				ClickResend={this.ClickResend}
				code={this.state.code}
				OnChangeCode={this.OnChangeCode}
				isLoading={this.state.loading}
				errMsg={this.state.errMsg}
				errCode={this.state.errCode}
			/>
		);
	}
}
