import React from "react";
import { number_format } from "../common/common";

/* eslint-disable */
class HistoryBody extends React.Component {
	onCancel(transId) {
		if (confirm(i18n.lbl_CancelConfirm)) {
			this.props.onCancel(transId);
		}
	}

	render() {
		const DepositStatus = {
			Pending: 1,
			Paid: 2,
			Approved: 3,
			Rejected: 4,
			Canceled: 5,
			Expired: 6,
			OnHold: 7
		};

		let data = this.props.Data;
		let rows = data.map(
			function (item, index) {
				let dateMonent = moment(item.createTime);
				let btnGroup = null;
				let statusGroup = null;

				if (
					item.depositType == 3 &&
					(item.status == 1 || item.status == 7)
				) {
					btnGroup = (
						<div className="btn-group">
							<button
								className="btn btn-default"
								type="button"
								onClick={this.onCancel.bind(
									this,
									item.transId
								)}>
								<span className="text">{i18n.lbl_Cancel}</span>
							</button>
							<button className="btn btn-secondary" type="button">
								<span className="text">
									{i18n.lbl_SubmitProof}
								</span>
							</button>
						</div>
					);
				}

				if (
					item.status == DepositStatus.Pending ||
					item.status == DepositStatus.Approved ||
					item.status == DepositStatus.Rejected ||
					item.status == DepositStatus.Canceled
				) {
					statusGroup = (
						<span className="text">
							{i18n["lbl_DepositStatus" + item.status]}
						</span>
					);
				} else {
					statusGroup = (
						<span className="text">{i18n.lbl_DepositStatus8}</span>
					);
				}

				return (
					<div className="group" key={index}>
						<div className="toggle option-header">
							<span>
								{dateMonent.format("l")} (
								{dateMonent.format("Z")})
							</span>
							<i className="icon icon-arrow-down"></i>
						</div>
						<div className="history">
							<div className="history-item">
								<span>{i18n["lbl_Time"]}</span>
								<span className="text">
									{dateMonent.format("hh:mm:ss A")}
								</span>
							</div>
							<div className="history-item">
								<span>{i18n["lbl_ID"]}</span>
								<span className="text">{item.transId}</span>
							</div>
							<div className="history-item">
								<span>{i18n["lbl_Method"]}</span>
								<span className="text">
									{i18n["lbl_DepositType" + item.depositType]}
								</span>
							</div>
							<div className="history-item">
								<span>{i18n["lbl_Amount"]}</span>
								<span className="text">
									{number_format(item.amount.toFixed(2))}
								</span>
							</div>
							<div className="history-item">
								<span>{i18n["lbl_Status"]}</span>
								{statusGroup}
								{btnGroup}
							</div>
						</div>
					</div>
				);
			}.bind(this)
		);

		let dateStr = moment(data[0].createTime).format("YYYY/MM/DD");
		return (
			<div className="tbody" data-date={dateStr} data-open="true">
				{rows}
			</div>
		);
	}
}

export default class DepositHistory extends React.Component {
	constructor(props) {
		super(props);
		this.state = { refresh: false };
	}

	onCancel(transId) {
		axios
			.post("/api/Payment/SetDepositCancel", { TransId: transId })
			.then(
				function (response) {
					let result = response.data;
					if (result.errorCode == 0) {
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

	render() {
		let data = this.props.Data;
		let bodyData = null;
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

			bodyData = dataContainer.map(
				function (item, index) {
					return (
						<HistoryBody
							key={index}
							Data={item.Data}
							onCancel={this.onCancel.bind(this)}
						/>
					);
				}.bind(this)
			);
		}

		return data != null && data.length > 0 ? (
			<div className="history-section">
				<div className="option-header">
					<span className="text-bold">
						{i18n.lbl_TransactionHistory}
					</span>
				</div>
				{bodyData}
			</div>
		) : (
			<div className="no-data">
				<span className="text">{i18n.msg_NoData}</span>
			</div>
		);
	}
}
/* eslint-enable */
