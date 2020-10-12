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
			.post("/api/AccountStrict/SendOTPSMS")
			.then(
				res => {
					let errMsg = "";
					let now = Math.floor(Date.now() / 1000);
					let startUnixTime = res.data.data.startUnixTime;

					if (res.data.errorCode !== 0) {
						let _errMsg =
							i18n[`msg_OTP_SendOTPSMS_${res.data.errorCode}`];
						errMsg =
							res.data.message ||
							_errMsg ||
							`[${res.data.errorCode}] ${i18n.msg_ApiServerError}`;
					}

					this.setState({
						loading: false,
						executeTime: now,
						startUnixTime: startUnixTime,
						errMsg: errMsg,
						errCode: res.data.errorCode
					});
				},
				err => {
					this.setState({
						errCode: 999,
						loading: false,
						errMsg: `[999] ${i18n.msg_ApiServerError}`
					});
				}
			);
	};

	ClickVerify = async () => {
		if (!this.state.code) {
			this.setState({ errMsg: i18n.msg_EnterOTPCode });
		} else {
			this.setState({ loading: true });
			await WebAPI()
				.post("/api/AccountStrict/VerifyOTP", {
					inputOTPcode: this.state.code
				})
				.then(
					res => {
						let errMsg = "";
						let now = Math.floor(Date.now() / 1000);
						if (res.data.errorCode === 0) {
							location.reload();
						} else {
							let _errMsg =
								i18n[
									`msg_OTP_SendOTPSMS_${res.data.errorCode}`
								];
							errMsg =
								res.data.message ||
								_errMsg ||
								`[${res.data.errCode}] ${i18n.msg_ApiServerError}`;

							this.setState({
								executeTime: now,
								errMsg: errMsg,
								loading: false
							});
						}
					},
					err => {
						this.setState({
							loading: false,
							errMsg: `[999] ${i18n.msg_ApiServerError}`
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
				startUnixTime={this.state.startUnixTime}
				executeTime={this.state.executeTime}
			/>
		);
	}
}
