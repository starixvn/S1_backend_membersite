import React, { Component } from "react";
import PopupOTP from "./popup.otp";
import WebAPI from "../../service/api/base";

(function ($, React, ReactDOM, i18n, axios, window) {
	class Panel extends Component {
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

		ClickClose = () => {
			$("#OTPPopupContent").attr("data-open", "false");
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
								$("#OTPPopupContent").attr(
									"data-open",
									"false"
								);
								location.reload();
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

		ClickResend = () => {
			this.SendVerifySMS();
		};

		OnChangeCode = e => {
			let value = e.target.value;
			this.setState({ code: value });
		};

		render() {
			if (!window.SendVerifySMS) {
				window.SendVerifySMS = this.SendVerifySMS;
			}
			return (
				<PopupOTP
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

	ReactDOM.render(<Panel />, document.getElementById("OTPPopup"));
})(jQuery, React, ReactDOM, i18n, axios, window);
