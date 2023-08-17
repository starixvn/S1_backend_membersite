import React, { Component } from "react";
import { number_format } from "../common/common";

const defAmountType = {
	input: "input",
	list: "list"
};

class SubmitMobilePay extends Component {
	constructor(props) {
		super(props);
		this.state = {
			MobilePayInfoList: null,
			selectPayment: null,
			channelId: null,
			Amount: "",
			amountType: null
		};
	}
	SelectAmount = amount => {
		this.setState({ Amount: amount });
	};
	OnChange = e => {
		this.setState({ Amount: e.target.value });
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
		value =
			IsConvertRequired === true &&
			this.state.amountType === defAmountType.input
				? value * 1000
				: value;
		let data = this.state.selectPayment;
		if (data === null) {
			status = 4;
			message = "Please select payment";
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
	AmountOnBlur = e => {
		let value = e.target.value;
		let checked = this.handleCheckAmount(value);
		if (checked.status === 3) {
			this.setState({
				Amount: value
			});
		} else {
			alert(checked.message);
		}
	};
	OnSubmit = () => {
		let checked = this.handleCheckAmount(this.state.Amount);
		if (checked.status == 3) {
			this.onConfirm();
		} else {
			alert(checked.message);
		}
	};
	onConfirm = () => {
		let checked = this.handleCheckAmount(this.state.Amount);
		if (checked.status === 3) {
			let mobilePayInfoList = this.props.MobilePayInfoList;
			let channelId = this.state.channelId;
			let mobilePayInfo = null;

			for (let i = 0; i < mobilePayInfoList.length; i++) {
				if (mobilePayInfoList[i].channelId == channelId) {
					mobilePayInfo = mobilePayInfoList[i];
					break;
				}
			}

			let real_amount =
				IsConvertRequired === true &&
				this.state.amountType === defAmountType.input
					? this.state.Amount * 1000
					: this.state.Amount;

			window.open(
				MobilePayUrl +
					"?ChannelId=" +
					mobilePayInfo.channelId +
					"&Amount=" +
					real_amount
			);

			this.props.ClickBack();
		} else {
			alert(checked.message);
		}
	};
	onSelectChannel = data => {
		let amountType =
			data.amountList?.length > 0
				? defAmountType.list
				: defAmountType.input;
		this.setState({
			selectPayment: data,
			channelId: data.channelId,
			amountType: amountType
		});
	};
	render() {
		let rows = this.props.MobilePayInfoList?.map(b => {
			return (
				<div
					className="card"
					onClick={this.onSelectChannel.bind(this, b)}
					key={`onlineBank-${b.groupId}-${b.channelName}`}>
					<div
						className={`option-item option-bank${
							this.state.channelId === b.channelId
								? " bank-selected"
								: ""
						}`}>
						<img
							src={
								"/common/images/MobilePay/" +
								b.groupName +
								".png"
							}
						/>
					</div>
				</div>
			);
		});

		let amountList = null;
		let amountInput = null;
		if (this.state.amountType === defAmountType.list) {
			let amountListRows = this.state.selectPayment?.amountList
				?.filter(m => {
					return (
						m >= this.state.selectPayment.minAmount &&
						m <= this.state.selectPayment.maxAmount
					);
				})
				?.map(m => {
					return (
						<span
							key={`mobilepay-${this.state.selectPayment.payId}-${this.state.selectPayment.channelId}-${m}`}
							className={`value-item${
								m === this.state.Amount ? " selected" : ""
							}`}
							onClick={this.SelectAmount.bind(this, m)}>
							{number_format(m)}
						</span>
					);
				});

			amountList = (
				<div className="mobile-options form-group">
					<div className="value-block">{amountListRows}</div>
				</div>
			);
		} else if (this.state.amountType === defAmountType.input) {
			amountInput = (
				<div className="form-group">
					{this.state.selectPayment === null ||
					(this.state.selectPayment?.minAmount == 0 &&
						this.state.selectPayment?.maxAmount == 0) ? (
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
									this.state.selectPayment?.minAmount
								)}{" "}
								~{" "}
								{number_format(
									this.state.selectPayment?.maxAmount
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
			);
		}

		return (
			<div className="options-method">
				<div className="toggle option-header">
					<span className="text-bold link-back">
						{i18n.lbl_MobileDeposit}
					</span>
					<span className="text-bold"></span>
				</div>
				<div className="options-card">{rows}</div>
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
				{amountList}
				<div className="form">
					{amountInput}
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

export default class MobilePay extends Component {
	constructor(props) {
		super(props);
		this.state = { MobilePayGroup: false, MobilePayInfoList: null };
	}
	componentDidMount() {
		this.getMobilePayGroup();
	}
	onMobilePay = payGroup => {
		this.getMobilePayInfoList(payGroup);
	};
	ClickBack = () => {
		this.setState({
			MobilePayGroup: false,
			groupId: null,
			MobilePayInfoList: null
		});
		this.props.hangleSubPage(false, null);
	};
	getMobilePayGroup() {
		axios
			.post("/api/Payment/GetMobilePayGroupList")
			.then(
				function (response) {
					let result = response.data;
					if (result.errorCode == 0) {
						let mobilePayGroupList = null;
						if (result.data.length > 0) {
							mobilePayGroupList = result.data;
						}

						this.setState({
							MobilePayGroupList: mobilePayGroupList
						});
					} else {
					}
				}.bind(this)
			)
			.catch(function (error) {}.bind(this));
	}

	getMobilePayInfoList = group => {
		let data = { GroupId: group.groupId };
		axios
			.post("/api/Payment/GetMobilePayInfoList", data)
			.then(
				function (response) {
					let result = response.data;
					if (result.errorCode == 0) {
						let mobilePayInfoList = null;
						let channelId = 0;
						if (result.data.length > 0) {
							mobilePayInfoList = result.data;
							channelId = mobilePayInfoList[0].channelId;
						}
						this.setState({
							MobilePayInfoList: mobilePayInfoList,
							channelId: channelId,
							groupId: group.groupId,
							MobilePayGroup: true
						});
						this.props.hangleSubPage(true, 2);
					} else {
						alert(result.message);
					}
				}.bind(this)
			)
			.catch(
				function (error) {
					alert(error);
				}.bind(this)
			);
	};
	render() {
		let payRows =
			this.state.MobilePayGroupList &&
			this.state.MobilePayGroupList?.map(d => {
				return (
					<li
						className="card"
						onClick={this.onMobilePay.bind(this, d)}
						key={`mobilepay-${d.groupId}`}>
						<div className="option-item">
							<img
								src={
									"/common/images/MobilePay/" +
									d.groupName +
									".png"
								}
							/>
							<div className="value">
								<p className="text-gray">
									{i18n["lbl_" + d.groupName]}
								</p>
							</div>
						</div>
					</li>
				);
			});

		return this.state.MobilePayGroup === false ||
			this.state.MobilePayInfoList === null ? (
			<div>
				<div className="method-item">
					<div className="toggle option-header">
						<span className="text-bold">
							{i18n.lbl_MobileDeposit} -
						</span>
						<span className="text-italic">
							{i18n.lbl_CreditTime}: {i18n.lbl_Instant}
						</span>
						<i className="icon icon-arrow-down"></i>
					</div>
					<ul className="options-card mobile-banking">{payRows}</ul>
				</div>
			</div>
		) : (
			<SubmitMobilePay
				ClickBack={this.ClickBack}
				MobilePayInfoList={this.state.MobilePayInfoList}
				channelId={this.state.channelId}
				GroupId={this.state.groupId}
			/>
		);
	}
}
