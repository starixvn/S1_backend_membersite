import React, { Component } from "react";

class SubmitLocalBank extends Component {
	componentDidMount() {
		axios
			.post("/api/Payment/GetBankInfoOffList")
			.then(
				function (response) {
					let result = response.data;
					if (result.errorCode == 0) {
						let BankInfoOff = null;
						if (result.data.length > 0) {
							BankInfoOff = result.data[0];
						}

						this.setState({
							BankInfoOffList: result.data,
							PickBankInfoOff: BankInfoOff
						});
					} else {
						alert(result.message);
						this.onDepositOptions();
					}
				}.bind(this)
			)
			.catch(
				function (error) {
					alert(error);
					this.onDepositOptions();
				}.bind(this)
			);
	}
	render() {
		return (
			<div className="options-method">
				<div className="toggle option-header">
					<span className="text-bold link-back">
						{i18n.lbl_WireTransfers}:
					</span>
					<span className="text-bold">Select bank</span>
				</div>
				<div className="form">
					<div className="form-group form-transfer">
						<div className="label">
							<span className="text-bold">Bank name: </span>
						</div>
						<div className="input-field">
							<div className="input-group bank-selection">
								<div className="bank-detail">
									<i className="icon logo-vcb"></i>
									<span className="text-bold">
										Vietcombank
									</span>
								</div>
								<div className="bank-limit">
									<span>Limit: </span>
									<span className="text-green">
										300('000) ~ 300,000 ('000)
									</span>
									<i className="icon icon-arrow-down"></i>
								</div>
							</div>
						</div>

						<div className="label">
							<span className="text-bold">Account number: </span>
						</div>
						<div className="input-field">
							<input
								className="input-control text-bold"
								type="text"
							/>
						</div>

						<div className="label">
							<span className="text-bold">Amount: </span>
						</div>
						<div className="input-field">
							<input
								className="input-control text-bold text-green"
								type="text"
							/>
						</div>
					</div>

					<div className="button-group">
						<button
							className="btn btn-primary"
							id="btn-back"
							onClick={this.props.ClickBack}>
							Back
						</button>
						<button className="btn btn-secondary">Submit</button>
					</div>
				</div>
			</div>
		);
	}
}

export default class LocalBank extends Component {
	constructor(props) {
		super(props);
		this.state = { LocalBank: false, subPage: false };
	}
	componentDidMount() {
		this.getLocalBank();
	}
	getLocalBank = () => {
		axios.post("/api/Payment/GetBankInfoOffList").then(
			function (response) {
				let result = response.data;
				if (result.errorCode == 0) {
					let localBank = false;
					if (result.data.length > 0) {
						localBank = true;
					}

					this.setState({ LocalBank: localBank });
				}
			}.bind(this)
		);
	};
	onClickLocalBank = () => {
		this.setState({ subPage: true });
		this.props.hangleSubPage(true, 1);
	};
	ClickBack = () => {
		this.setState({ subPage: false });
		this.props.hangleSubPage(false, null);
	};
	render() {
		return this.state.LocalBank ? (
			this.state.subPage ? (
				<SubmitLocalBank ClickBack={this.ClickBack} />
			) : (
				<div className="method-item">
					<div className="toggle option-header">
						<span className="text-bold">
							{i18n.lbl_WireTransfers} -
						</span>
						<span className="text-italic">
							{i18n.lbl_CreditTime}: {i18n.lbl_5mins}
						</span>
						<i className="icon icon-arrow-down"></i>
					</div>
					<ul className="options-card local-transfer">
						<li className="card" onClick={this.onClickLocalBank}>
							<div className="option-item">
								<img src="/common/images/Local_Bank.png" />
								<div className="value">
									<p className="text-gray">
										{i18n.lbl_WireTransfers}
									</p>
								</div>
							</div>
						</li>
					</ul>
				</div>
			)
		) : (
			<></>
		);
	}
}
