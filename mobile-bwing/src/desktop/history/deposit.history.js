import React from "react";
import { number_format } from "../../common/common";

/* eslint-disable */
class HistoryBody extends React.Component {
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
						<div className="button-group">
							<button
								className="btn btn-primary"
								type="button"
								onClick={this.props.onCancel.bind(
									this,
									item.transId
								)}>
								<span className="text">{i18n.lbl_Cancel}</span>
							</button>
							<button
								className="btn btn-secondary"
								type="button"
								onClick={this.props.onShowBankModal.bind(this, {
									...item.depositRep,
									...item.bankInfo,
									amount: item.amount,
									postScript: item.postScript,
									transId: item.transId,
									refId: item.refId,
									depositTime: item.depositTime,
									picFileName: item.picFileName
								})}>
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
					<tr key={index}>
						<td className="td-date">
							{dateMonent.format("l")} ({dateMonent.format("Z")})
						</td>
						<td className="td-time">
							{dateMonent.format("hh:mm:ss A")}
						</td>
						<td className="td-id">{item.transId}</td>
						<td className="td-method">
							{i18n["lbl_DepositType" + item.depositType]}
						</td>
						<td className="td-amount">
							{number_format(item.amount.toFixed(2))}
						</td>
						<td className="td-status">{statusGroup}</td>
						<td className="td-btn">{btnGroup}</td>
					</tr>
				);
			}.bind(this)
		);

		//let dateStr = moment(data[0].createTime).format("YYYY/MM/DD");
		return rows;
	}
}

export default class DepositHistory extends React.Component {
	constructor(props) {
		super(props);
		this.state = { refresh: false };
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
							onCancel={this.props.onCancel.bind(this)}
							onShowBankModal={this.props.onShowBankModal}
						/>
					);
				}.bind(this)
			);
		}

		return data != null && data.length > 0 ? (
			<div className="table-content">
				<div className="option-header">
					<span className="text-bold">
						{i18n.lbl_TransactionHistory}
					</span>
				</div>
				<table>
					<thead>
						<tr>
							<th className="td-date">{i18n.lbl_Date}</th>
							<th className="td-time">{i18n.lbl_Time}</th>
							<th className="td-id">{i18n.lbl_ID}</th>
							<th className="td-method">{i18n.lbl_Method}</th>
							<th className="td-amount">{i18n.lbl_Amount}</th>
							<th className="td-status">{i18n.lbl_Status}</th>
							<th className="td-btn"></th>
						</tr>
					</thead>
					<tbody>{bodyData}</tbody>
				</table>
			</div>
		) : (
			<div className="no-data">
				<span className="text">{i18n.msg_NoData}</span>
			</div>
		);
	}
}
/* eslint-enable */
