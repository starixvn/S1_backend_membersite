import React, { Component } from "react";
import { number_format } from "../common/common";

class SubmitOnlineBanking extends Component {
	constructor(props) {
		super(props);
		this.state = { SelectBank: null };
	}
	OnSelect = bank => {
		axios
			.post("/api/Payment/GetOnlineBankInfo", {
				payId: this.props.PayId,
				sysId: bank.sysId
			})
			.then(
				function (response) {
					let result = response.data;
					if (result.errorCode == 0) {
						this.setState({ SelectBank: result.data });
					}
				}.bind(this)
			);
	};
	OnBlur = e => {
		let value = e.target.value;
		this.setState({
			Amount: value
		});
	};
	handleCheckAmount = value => {
		let status = 1;
		let message = "";
		let data = this.state.SelectBank;
		value = IsConvertRequired === true ? value * 1000 : value;
		if (data === null) {
			status = 4;
			message = i18n.lbl_PleaseSelBank;
		} else if (isNaN(value)) {
			status = 2;
			message = i18n.lbl_AmountTooltip;
		} else if (value <= 0) {
			status = 2;
			message = i18n.lbl_AmountTooltip;
		} else if (
			data.minAmount != 0 &&
			data.maxAmount != 0 &&
			(value > data.maxAmount || value < data.minAmount)
		) {
			status = 2;
			message = number_format(value) + " " + i18n.lbl_NotinLimit;
		} else {
			status = 3;
			message = "";
		}

		return { status: status, message: message };
	};
	OnSubmit = () => {
		let checked = this.handleCheckAmount(this.state.Amount);
		if (checked.status == 3) {
			let real_amount =
				IsConvertRequired === true
					? this.state.Amount * 1000
					: this.state.Amount;
			this.props.OnSubmit(real_amount, this.state.SelectBank.sysId);
		} else {
			alert(checked.message);
		}
	};
	render() {
		let bankRows = this.props.OnlineBankInfo?.map(b => {
			return (
				<div
					className="card"
					onClick={this.OnSelect.bind(this, b)}
					key={`onlineBank-${b.sysId}`}>
					<div
						className={`option-item option-bank${
							this.state.SelectBank?.sysId === b.sysId
								? " bank-selected"
								: ""
						}`}>
						<img src={"/common/images/BankLogo/" + b.bankLogo} />
					</div>
				</div>
			);
		});
		return (
			<div className="options-method">
				<div className="toggle option-header">
					<span className="text-bold link-back">
						{i18n.lbl_OnlineBanking}
					</span>
					<span className="text-bold"></span>
				</div>
				<div className="options-card">{bankRows}</div>
				<div className="option-header">
					<span className="text-bold">
						<span className="text-bold">
							{i18n.lbl_InputAmount}:{" "}
						</span>
						<span className="text-italic">
							{i18n.lbl_DependOnTheBank}
						</span>
					</span>
				</div>
				<div className="form">
					<div className="form-group form-transfer">
						{this.state.SelectBank === null ||
						(this.state.SelectBank?.minAmount == 0 &&
							this.state.SelectBank?.maxAmount == 0) ? (
							<div className="label">
								<span className="text-bold">
									{i18n.lbl_Limit}:{" "}
								</span>{" "}
								<br />
								<span className="text-bold text-green">
									{i18n.lbl_Unlimited}
								</span>
							</div>
						) : (
							<div className="label">
								<span className="text-bold">
									{i18n.lbl_Limit}:{" "}
								</span>{" "}
								<br />
								<span className="text-bold text-green">
									{number_format(
										this.state.SelectBank?.minAmount
									)}{" "}
									~{" "}
									{number_format(
										this.state.SelectBank?.maxAmount
									)}
								</span>
							</div>
						)}
						<div className="input-field">
							<input
								className="input-control text-green text-right"
								type="number"
								pattern="[0-9]*"
								onBlur={this.OnBlur}
							/>
							{IsConvertRequired ? (
								<span className="thousands text-green text-bold">
									('000)
								</span>
							) : null}
						</div>
					</div>

					<div className="button-group">
						<button
							className="btn btn-primary"
							id="btn-back"
							onClick={this.props.ClickBack}>
							{i18n.lbl_Back}
						</button>
						<button
							className="btn btn-secondary"
							onClick={this.OnSubmit}>
							{i18n.lbl_Submit}
						</button>
					</div>
				</div>
			</div>
		);
	}
}

export default class OnlineBanking extends Component {
	constructor(props) {
		super(props);
		this.state = {
			OnlineBankInfo: [],
			OnlinePayment: [],
			payId: null,
			BankInfoPay: null
		};
	}
	componentDidMount() {
		this.getOnlinePayment();
	}
	getOnlineBanking = paymentInfo => {
		let data = { payId: paymentInfo.payId };
		axios.post("/api/Payment/GetOnlineBankInfoSysList", data).then(
			function (response) {
				let result = response.data;
				if (result.errorCode == 0) {
					let onlineBankInfo = null;
					if (result.data.length > 0) {
						onlineBankInfo = result.data;
					}

					this.setState({ OnlineBankInfo: onlineBankInfo });
					this.setState({
						payId: paymentInfo.payId,
						BankInfoPay: paymentInfo
					});
					this.props.hangleSubPage(true, 3);
				}
			}.bind(this)
		);
	};
	getOnlinePayment = () => {
		axios
			.post("/api/Payment/GetOnlinePaymentList")
			.then(
				function (response) {
					let result = response.data;
					if (result.errorCode == 0) {
						let onlinePayment = null;
						if (result.data.length > 0) {
							onlinePayment = result.data;
						}

						this.setState({ OnlinePayment: onlinePayment });
					} else {
					}
				}.bind(this)
			)
			.catch(function (error) {}.bind(this));
	};
	onOnlinePayment = paymentInfo => {
		this.getOnlineBanking(paymentInfo);
	};
	ClickBack = () => {
		this.setState({ payId: null, BankInfoPay: null, OnlineBankInfo: [] });
		this.props.hangleSubPage(false, null);
	};
	onConfirm = (Amount, sysId) => {
		let bankInfoPay = this.state.BankInfoPay;
		window.open(
			OnlineBankingUrl +
				"?sysId=" +
				sysId +
				"&payId=" +
				bankInfoPay.payId +
				"&ChannelId=" +
				bankInfoPay.channelId +
				"&Amount=" +
				Amount
		);
	};
	render() {
		let rows = this.state.OnlinePayment?.map(d => {
			return (
				<li
					className="card"
					key={`${d.payId}-${d.channelId}-${d.payName}`}
					onClick={this.onOnlinePayment.bind(this, d)}>
					<div className="option-item">
						<img
							src={"/common/images/Payment/" + d.payName + ".png"}
							alt={d.payName}
						/>
						{d.minAmount == 0 && d.maxAmount == 0 ? (
							<div className="value">
								<p className="text-gray">{i18n.lbl_Limit}:</p>
								<p className="green">{i18n.lbl_Unlimited}</p>
							</div>
						) : (
							<div className="value">
								<p className="text-gray">{i18n.lbl_Limit}:</p>
								<p className="green">
									{number_format(d.minAmount)} ~{" "}
									{number_format(d.maxAmount)}
								</p>
							</div>
						)}
					</div>
				</li>
			);
		});

		return this.state.payId === null ? (
			<div className="method-item">
				<div className="toggle option-header">
					<span className="text-bold">
						{i18n.lbl_OnlineBanking} -
					</span>
					<span className="text-italic">
						{i18n.lbl_CreditTime}: {i18n.lbl_Instant}
					</span>
					<i className="icon icon-arrow-down"></i>
				</div>
				<ul className="options-card online-baking">{rows}</ul>
			</div>
		) : (
			<SubmitOnlineBanking
				ClickBack={this.ClickBack}
				OnSubmit={this.onConfirm}
				PayId={this.state.payId}
				OnlineBankInfo={this.state.OnlineBankInfo}
			/>
		);
	}
}
