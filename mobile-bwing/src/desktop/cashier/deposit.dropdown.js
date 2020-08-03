import React from "react";
import { number_format } from "../../common/common";

export default class BankInfoOffDropdown extends React.Component {
	constructor(props) {
		super(props);
		this.state = { dropdown: false };
		this.Dropdown = React.createRef();
	}
	onPickBankInfoOff(PickBankInfoOff) {
		this.props.onPickBankInfoOff(PickBankInfoOff);
		this.setState({ dropdown: false });
	}

	onDropdown() {
		let node = event.target;
		let parentNode = node.parentNode;
		if (
			node.className != "btn btn-icon btn-form-status" &&
			parentNode != undefined &&
			parentNode.className != "btn btn-icon btn-form-status"
		) {
			let dropdownVal = !this.state.dropdown;
			let $target = $(this.Dropdown.current);

			if (typeof $target.attr("data-open") == "undefined") {
				$target.attr("data-open", true);
				dropdownVal = true;
			}

			this.setState({ dropdown: dropdownVal });
		}
	}

	render() {
		let PickBankInfoOff = this.props.PickBankInfoOff;
		let DropdownItem = this.props.BankInfoOffList.map(
			function (data) {
				return (
					<div
						className="item"
						key={data.sysId}
						onClick={this.onPickBankInfoOff.bind(this, data)}
						data-selected={data.sysId == PickBankInfoOff.sysId}>
						<img
							src={"/common/images/BankLogo/" + data.bankLogo}
							className="name"
						/>
						<span className="value">{data.bankName}</span>
						<span className="limit">
							(Limit:{" "}
							<span className="text-green">
								{data.minAmount == 0 && data.maxAmount == 0
									? i18n.lbl_Unlimited
									: `${number_format(
											data.minAmount
									  )} ~ ${number_format(data.maxAmount)}`}
							</span>{" "}
							)
						</span>
					</div>
				);
			}.bind(this)
		);

		return (
			<div
				className="input-field dropdown ddl-default"
				ref={this.Dropdown}>
				<div
					className="selected-text input-control"
					onClick={this.onDropdown.bind(this)}>
					<img
						src={
							"/common/images/BankLogo/" +
							PickBankInfoOff.bankLogo
						}
						className="name"
					/>
					<span className="value">{PickBankInfoOff.bankName}</span>
					<span className="limit">
						(Limit:{" "}
						<span className="text-green">
							{PickBankInfoOff.minAmount == 0 &&
							PickBankInfoOff.maxAmount == 0
								? i18n.lbl_Unlimited
								: `${number_format(
										PickBankInfoOff.minAmount
								  )} ~ ${number_format(
										PickBankInfoOff.maxAmount
								  )}`}
						</span>{" "}
						)
					</span>

					<span className="icon-action active">
						<i className="icon icon-arrow-down"></i>
					</span>
				</div>
				<div
					className={
						"dropdown-container" +
						(this.state.dropdown ? "" : " hide")
					}>
					{DropdownItem}
				</div>
			</div>
		);
	}
}
