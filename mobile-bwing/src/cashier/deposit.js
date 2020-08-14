import React, { Component } from "react";
import ReactDOM from "react-dom";
import OnlineBanking from "./deposit.onlineBanking";
import LocalBank from "./deposit.localBank";
import MobilePay from "./deposit.mobilePay";
import RecentHistoryBody from "./deposit.history";

class DepositPanel extends Component {
	constructor(props) {
		super(props);
		this.state = { clickSub: false, history: [], page: null };
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
					return <RecentHistoryBody key={index} Data={item.Data} />;
				}.bind(this)
			);
		}

		return (
			<div className="content deposit-block">
				<p className="txt-note">{i18n["lbl_DepositOptionHint"]}</p>
				<div className={this.state.clickSub ? "" : "options-method"}>
					{this.state.page === null || this.state.page === 3 ? (
						<OnlineBanking hangleSubPage={this.hangleSubPage} />
					) : null}

					{this.state.page === null || this.state.page === 1 ? (
						<LocalBank hangleSubPage={this.hangleSubPage} />
					) : null}

					{this.state.page === null || this.state.page === 2 ? (
						<MobilePay hangleSubPage={this.hangleSubPage} />
					) : null}
				</div>
				{this.state.clickSub ? null : (
					<div className="history-section">
						<div className="option-header">
							<span className="text-bold">
								{i18n.lbl_RecentTransactions}
							</span>
						</div>
						{historyBodyData}
						<div className="button-block text-right">
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
		);
	}
}

export default DepositPanel;

if (process.env.NODE_ENV === "production") {
	ReactDOM.render(<DepositPanel />, document.getElementById("content"));
}
