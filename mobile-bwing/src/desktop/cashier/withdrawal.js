import React, { Component } from "react";
import { number_format } from "../../common/common";

class BankList extends Component {
	constructor(props) {
		super(props);
		this.state = { bankName: "" };
	}
	onPickBankInfo = bankInfo => {
		this.props.onPickBankInfo(bankInfo);
		this.setState({ bankName: bankInfo.bankName });
	};
	render() {
		let rows =
			this.props.BankInfoDetail &&
			this.props.BankInfoDetail.map(b => {
				let checked = this.state.bankName === b.bankName;
				let id = `rb-${b.bankName}`;
				return (
					<div
						className="card"
						key={`bankd-${b.bankName}`}
						onClick={this.onPickBankInfo.bind(this, b)}>
						<div className="option-item">
							<img
								src={"/common/images/BankLogo/" + b.bankLogo}
								alt={b.bankName}
							/>
							<div className="radio-button">
								<input
									type="radio"
									id={id}
									name="bank"
									checked={checked}
									onChange={() => {}}
								/>
								<label htmlFor={id}></label>
							</div>
						</div>
					</div>
				);
			});
		return <div className="option-content">{rows}</div>;
	}
}

class WithdrawalPanel extends Component {
	constructor(props) {
		super(props);
		this.state = {
			BankInfoDetail: [],
			AcountName: "",
			PickBankInfo: null,
			Amount: null
		};
	}
	componentDidMount() {
		this.getWithdrawalAvailableBalance();
		this.getCustBankInfo();
	}
	getCustBankInfo = () => {
		axios
			.post("/api/Account/GetCustBankInfo")
			.then(
				function (response) {
					let result = response.data;
					if (
						result.errorCode == 0 &&
						result.data.bankInfoDetail.length > 0
					) {
						this.setState({
							BankInfoDetail: result.data.bankInfoDetail,
							AcountName: result.data.accountName
						});
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
	getWithdrawalAvailableBalance = () => {
		axios
			.post("/api/Account/GetWithdrawalAvailableBalance")
			.then(
				function (response) {
					let result = response.data;
					if (result.errorCode == 0) {
						this.setState({
							Balance: result.data.balance,
							Deduction: result.data.deduction,
							AvailableBalance: result.data.availableBalance
						});
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
	onConfirm = () => {
		if (this.state.PickBankInfo == null) {
			alert(i18n.lbl_PleaseSelBank);
		} else if (
			this.state.Amount == null ||
			isNaN(this.state.Amount) ||
			Number(this.state.Amount) < 0
		) {
			alert("Amount can not be less than 0");
		} else {
			let newAmount = IsConvertRequired
				? this.state.Amount * 1000
				: this.state.Amount;

			let data = {
				sysId: this.state.PickBankInfo.sysId,
				AccountNo: this.state.PickBankInfo.accountNo,
				amount: newAmount,
				memo: this.state.Memo
			};
			axios
				.post("/api/Payment/WithdrawalReq", data)
				.then(
					function (response) {
						let result = response.data;
						if (result.errorCode == 0) {
							alert(result.message);
						} else if (result.errorCode == -1) {
							alert(
								i18n.lbl_RolloverOver +
									": " +
									number_format(parseFloat(result.message))
							);
						} else if (result.errorCode == -2) {
							alert(
								i18n.lbl_DepositOver +
									": " +
									number_format(parseFloat(result.message))
							);
						} else if (result.errorCode == -3) {
							alert(
								i18n.lbl_WithdrawalOver +
									": " +
									number_format(parseFloat(result.message))
							);
						} else if (result.errorCode == 2) {
							window.location.replace(window.location.href);
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
		}
	};
	onPickBankInfo = bankInfo => {
		this.setState({ PickBankInfo: bankInfo });
	};

	handleCheckAmount = value => {
		let status = 1;
		let message = "";

		if (isNaN(value)) {
			status = 2;
			message = i18n.lbl_AmountTooltip;
		} else if (value <= 0) {
			status = 2;
			message = i18n.lbl_AmountTooltip;
		} else {
			status = 3;
			message = "";
		}

		return { status: status, message: message };
	};
	AmountOnBlur = e => {
		let value = e.target.value;
		let result = this.handleCheckAmount(e.target.value);
		if (result.status !== 3) {
			alert(result.message);
		} else {
			this.setState({ Amount: Number(value) });
		}
	};
	render() {
		return (
			<>
				<div className="title">{i18n.lbl_Withdrawal}</div>
				<div className="content">
					<div className="options-method">
						<div className="option-header">
							<span className="text-bold">
								{i18n.lbl_SelectBank}
							</span>
						</div>
						{this.state.BankInfoDetail ? (
							<BankList
								BankInfoDetail={this.state.BankInfoDetail}
								onPickBankInfo={this.onPickBankInfo}
							/>
						) : null}

						<div className="option-header">
							<span className="text-bold">
								<span className="text-bold">
									{i18n.lbl_RecipientInfo}:{" "}
								</span>
							</span>
						</div>
						<div className="form">
							<p className="note">
								<span className="text-red text-bold">
									{i18n.lbl_Note}:{" "}
								</span>
								<span>{i18n.lbl_WithdrawalNotice}</span>
							</p>
							<div className="form-group">
								<div className="label">
									<span className="text-bold">
										{i18n.lbl_AccountNumber}:{" "}
									</span>
								</div>
								<div className="input-field">
									<input
										className="input-control"
										type="number"
										readOnly
										value={
											this.state.PickBankInfo !== null
												? this.state.PickBankInfo
														.accountNo
												: ""
										}
									/>
								</div>

								<div className="label">
									<span className="text-bold">
										{i18n.lbl_RecipientName}:{" "}
									</span>
								</div>
								<div className="input-field">
									<input
										className="input-control"
										type="text"
										readOnly
										value={
											this.state.AcountName
												? this.state.AcountName
												: ""
										}
									/>
								</div>

								<div className="label">
									<span className="text-bold">
										{i18n.lbl_WithdrawalAmount}
									</span>
								</div>
								<div className="input-field">
									<input
										className="input-control text-green text-right"
										type="number"
										pattern="[0-9]*"
										onBlur={this.AmountOnBlur}
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
									onClick={this.onConfirm}>
									{i18n.lbl_Withdrawal}
								</button>
							</div>
						</div>
					</div>
				</div>
			</>
		);
	}
}

export default WithdrawalPanel;

if (process.env.NODE_ENV === "production") {
	ReactDOM.render(<WithdrawalPanel />, document.getElementById("content"));
}
