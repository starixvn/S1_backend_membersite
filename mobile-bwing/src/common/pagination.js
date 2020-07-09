import React from "react";

export default class Pagination extends React.Component {
	constructor(props) {
		super(props);
	}

	onPaging(value) {
		this.props.onPaging(value);
	}

	render() {
		let pageNumber = this.props.PageNumber || 0;
		let totalPages = this.props.TotalPages || 0;

		let pageRange = PageRange(pageNumber, totalPages);
		let PageNum = [];

		for (let i in pageRange) {
			PageNum.push(
				<a
					className={pageNumber == pageRange[i] ? "is-active" : ""}
					key={`page-${pageRange[i]}`}
					onClick={this.onPaging.bind(this, pageRange[i])}>
					<li>{pageRange[i]}</li>
				</a>
			);
		}

		return totalPages == 0 ? null : (
			<div className="paging">
				<div className="square-fill pagination">
					<ul>{PageNum}</ul>
				</div>
			</div>
		);
	}
}

const PageRange = (currentPage, totalPages) => {
	let pairs = 3,
		range = [1];

	if (totalPages <= 1) return range;

	let range_of_head = currentPage - pairs;
	let range_of_tail = currentPage + pairs;
	for (let i = range_of_head; i <= range_of_tail; i++) {
		if (i < totalPages && i > 1) range.push(i);
	}
	range.push(totalPages);

	let tmp,
		rangeWithDots = [];
	for (let i of range) {
		if (tmp) {
			if (i - tmp === 2) {
				rangeWithDots.push(tmp + 1);
			} else if (i - tmp !== 1) {
				rangeWithDots.push("...");
			}
		}
		rangeWithDots.push(i);
		tmp = i;
	}

	return rangeWithDots;
};
