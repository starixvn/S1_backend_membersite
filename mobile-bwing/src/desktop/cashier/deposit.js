import React, { Component } from "react";
import ReactDOM from "react-dom";
import OnlineBanking from "./deposit.onlineBanking";
import LocalBank from "./deposit.localBank";
import MobilePay from "./deposit.mobilePay";
import RecentHistoryBody from "./deposit.history";
import { BankInfoModal } from "./deposit.localBank";

class DepositPanel extends Component {
	constructor(props) {
		super(props);
		this.state = {
			clickSub: false,
			history: [],
			page: null,
			showBankInfoModal: false,
			transBankInfo: null,
			PickBankInfoOff: null,
			Amount: null
		};
	}
	componentDidMount() {
		this.GetDepositInfoRecent();
	}
	GetDepositInfoRecent = () => {
		axios
			.post("/api/Payment/GetDepositInfoRecent")
			.then(
				function (response) {
					let result = response.data;
					if (result.errorCode == 0) {
						this.setState({ history: result.data });
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
	hangleSubPage = (isSub, page) => {
		this.setState({ clickSub: isSub, page: page });
		$("html,body").scrollTop(0);
		if (isSub === false) {
			this.GetDepositInfoRecent();
		}
	};
	ViewAll = () => {
		window.location.href = hrefAllTrans;
	};
	onCancel = transId => {
		if (confirm(i18n.lbl_CancelConfirm)) {
			axios
				.post("/api/Payment/SetDepositCancel", { TransId: transId })
				.then(
					function (response) {
						let result = response.data;
						if (result.errorCode == 0) {
							let newData = [...this.state.history];
							let find_idx = newData.findIndex(
								x => x.transId === transId
							);
							newData[find_idx].status = 5;
							this.setState({ history: newData });
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
	onShowBankModal = datas => {
		this.setState({
			showBankInfoModal: true,
			transBankInfo: datas,
			PickBankInfoOff: datas,
			Amount: datas.amount
		});
	};
	render() {
		let data = this.state.history;
		let historyBodyData = null;
		if (data != null) {
			let dataContainer = [];
			for (let i = 0; i < data.length; i++) {
				let row = data[i];
				let mDate = moment(row.createTime);
				let sDate = mDate.format("YYYYMMDD");
				if (
					dataContainer.length == 0 ||
					dataContainer[dataContainer.length - 1].Date != sDate
				) {
					dataContainer.push({ Date: sDate, Data: [row] });
				} else {
					dataContainer[dataContainer.length - 1].Data.push(row);
				}
			}

			historyBodyData = dataContainer?.map(
				function (item, index) {
					return (
						<RecentHistoryBody
							key={index}
							Data={item.Data}
							onCancel={this.onCancel}
							onShowBankModal={this.onShowBankModal}
						/>
					);
				}.bind(this)
			);
		}

		return (
			<>
				<div className="title">{i18n.lbl_Deposit}</div>
				<div className="content">
					<p style={{ marginBottom: "2rem" }}>
						{i18n["lbl_DepositOptionHint"]}
					</p>

					<div className="section" id="optionDeposit">
						{this.state.page === null || this.state.page === 3 ? (
							<OnlineBanking hangleSubPage={this.hangleSubPage} />
						) : null}

						{this.state.page === null || this.state.page === 1 ? (
							<LocalBank hangleSubPage={this.hangleSubPage} />
						) : null}

						{this.state.page === null || this.state.page === 2 ? (
							<MobilePay hangleSubPage={this.hangleSubPage} />
						) : null}

						{this.state.clickSub ? null : (
							<div className="table-content">
								<div className="option-header">
									<span className="text-bold">
										{i18n.lbl_RecentTransactions}
									</span>
								</div>
								<table>
									<thead>
										<tr>
											<th className="td-date">
												{i18n.lbl_Date}
											</th>
											<th className="td-time">
												{i18n.lbl_Time}
											</th>
											<th className="td-id">
												{i18n.lbl_ID}
											</th>
											<th className="td-method">
												{i18n.lbl_Method}
											</th>
											<th className="td-amount">
												{i18n.lbl_Amount}
											</th>
											<th className="td-status">
												{i18n.lbl_Status}
											</th>
											<th></th>
										</tr>
									</thead>
									<tbody>{historyBodyData}</tbody>
								</table>
								<div className="button-block">
									<button
										className="btn btn-primary"
										type="button"
										onClick={this.ViewAll}>
										{i18n.lbl_AllTranscations}
									</button>
								</div>
							</div>
						)}
					</div>
				</div>
				{this.state.showBankInfoModal ? (
					<BankInfoModal
						bankInfo={this.state.transBankInfo}
						PickBankInfoOff={this.state.PickBankInfoOff}
						Amount={this.state.Amount}
					/>
				) : null}
			</>
		);
	}
}

export default DepositPanel;

if (process.env.NODE_ENV === "production") {
	ReactDOM.render(<DepositPanel />, document.getElementById("content"));
}
