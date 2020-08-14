import React, { Component } from "react";
import { number_format } from "../../common/common";

const DepositStatus = {
	Pending: 1,
	Paid: 2,
	Approved: 3,
	Rejected: 4,
	Canceled: 5,
	Expired: 6,
	OnHold: 7
};

export default class RecentHistoryBody extends React.Component {
	render() {
		let data = this.props.Data;

		let rows = data?.map(
			function (item) {
				let dateMonent = moment(item.createTime);
				let btnGroup = null;
				let statusGroup = null;

				if (
					item.depositType == 3 &&
					(item.status == DepositStatus.Pending ||
						item.status == DepositStatus.OnHold)
				) {
					btnGroup = (
						<div className="button-group">
							{item.status == DepositStatus.Pending ? (
								<button
									className="btn btn-primary"
									type="button"
									onClick={this.props.onCancel.bind(
										this,
										item.transId
									)}>
									<span className="text">
										{i18n.lbl_Cancel}
									</span>
								</button>
							) : null}
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
						<span>{i18n["lbl_DepositStatus" + item.status]}</span>
					);
				} else {
					statusGroup = <span>{i18n.lbl_DepositStatus8}</span>;
				}

				return (
					<tr key={`dp-history-${item.transId}`}>
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

		let dateStr = moment(data[0].createTime).format("YYYY/MM/DD");
		return rows;
	}
}
